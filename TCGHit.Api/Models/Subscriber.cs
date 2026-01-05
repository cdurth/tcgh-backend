namespace TCGHit.Api.Models;

/// <summary>
/// Represents an email subscriber for the coming soon page.
/// </summary>
public class Subscriber
{
    public int Id { get; set; }

    public required string Email { get; set; }

    public bool HasConsent { get; set; }

    public string? Source { get; set; }

    public DateTime SubscribedAt { get; set; }

    public string? IpAddress { get; set; }

    public bool IsActive { get; set; } = true;
}
