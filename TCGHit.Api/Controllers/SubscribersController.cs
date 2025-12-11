using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TCGHit.Api.Data;
using TCGHit.Api.Models;

namespace TCGHit.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscribersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SubscribersController> _logger;

    public SubscribersController(AppDbContext context, ILogger<SubscribersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Subscribe an email address to the mailing list
    /// </summary>
    /// <param name="request">Subscription request</param>
    /// <returns>Subscription result</returns>
    [HttpPost]
    [EnableRateLimiting("subscription")]
    [ProducesResponseType(typeof(SubscribeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SubscribeResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SubscribeResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        // Honeypot check - reject if honeypot field has a value
        if (!string.IsNullOrEmpty(request.Website))
        {
            _logger.LogWarning("Honeypot triggered for email {Email}", request.Email);
            // Return success to not reveal bot detection
            return Ok(SubscribeResponse.SuccessResponse());
        }

        // Consent is required
        if (!request.ConsentGiven)
        {
            return BadRequest(SubscribeResponse.ErrorResponse("You must agree to receive emails to subscribe."));
        }

        // Normalize email to lowercase
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Check for existing subscriber
        var existingSubscriber = await _context.Subscribers
            .FirstOrDefaultAsync(s => s.Email == normalizedEmail);

        if (existingSubscriber != null)
        {
            if (existingSubscriber.IsActive)
            {
                _logger.LogInformation("Duplicate subscription attempt for {Email}", normalizedEmail);
                return Conflict(SubscribeResponse.AlreadySubscribedResponse());
            }

            // Reactivate inactive subscriber
            existingSubscriber.IsActive = true;
            existingSubscriber.ConsentGiven = request.ConsentGiven;
            existingSubscriber.SubscribedAt = DateTime.UtcNow;
            existingSubscriber.IpAddress = GetClientIpAddress();
            existingSubscriber.Source = request.Source ?? "landing-page";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Reactivated subscriber {Email}", normalizedEmail);
            return StatusCode(StatusCodes.Status201Created, SubscribeResponse.SuccessResponse("Welcome back! You've been re-subscribed."));
        }

        // Create new subscriber
        var subscriber = new Subscriber
        {
            Email = normalizedEmail,
            ConsentGiven = request.ConsentGiven,
            Source = request.Source ?? "landing-page",
            SubscribedAt = DateTime.UtcNow,
            IpAddress = GetClientIpAddress(),
            IsActive = true
        };

        _context.Subscribers.Add(subscriber);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New subscriber added: {Email} from {Source}", normalizedEmail, subscriber.Source);

        return StatusCode(StatusCodes.Status201Created, SubscribeResponse.SuccessResponse());
    }

    private string? GetClientIpAddress()
    {
        // Check for forwarded IP (when behind a proxy/load balancer)
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the list (original client)
            return forwardedFor.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
