using System.ComponentModel.DataAnnotations;

namespace TCGHit.Api.Models;

/// <summary>
/// Request model for email subscription
/// </summary>
public class SubscribeRequest
{
    /// <summary>
    /// Email address to subscribe
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [MaxLength(254, ErrorMessage = "Email address is too long")]
    public required string Email { get; set; }

    /// <summary>
    /// User's consent to receive marketing emails
    /// </summary>
    [Required]
    public bool ConsentGiven { get; set; }

    /// <summary>
    /// Source identifier (e.g., 'landing-page', 'popup')
    /// </summary>
    [MaxLength(50)]
    public string? Source { get; set; }

    /// <summary>
    /// Honeypot field for bot detection - should be empty
    /// </summary>
    public string? Website { get; set; }
}
