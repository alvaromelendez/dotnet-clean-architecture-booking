using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Api.Infrastructure;

/// <summary>
/// Fixed-window limits per client IP, bound from the <c>RateLimiting</c> configuration section.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, int.MaxValue)]
    public int PermitLimit { get; init; } = 100;

    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; init; } = 60;

    [Range(0, int.MaxValue)]
    public int QueueLimit { get; init; }
}
