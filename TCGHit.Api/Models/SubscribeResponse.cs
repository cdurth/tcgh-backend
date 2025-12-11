namespace TCGHit.Api.Models;

/// <summary>
/// Response model for email subscription
/// </summary>
public class SubscribeResponse
{
    /// <summary>
    /// Whether the subscription was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Whether the email was already subscribed (for 409 responses)
    /// </summary>
    public bool? AlreadySubscribed { get; set; }

    public static SubscribeResponse SuccessResponse(string message = "Successfully subscribed!") =>
        new() { Success = true, Message = message };

    public static SubscribeResponse AlreadySubscribedResponse() =>
        new() { Success = false, Message = "This email is already subscribed.", AlreadySubscribed = true };

    public static SubscribeResponse ErrorResponse(string message) =>
        new() { Success = false, Message = message };
}
