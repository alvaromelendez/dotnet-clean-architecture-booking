using BookingSystem.Application.Abstractions.Clock;

namespace BookingSystem.IntegrationTests.Api;

/// <summary>
/// Freezes "now" so HTTP tests can use fixed dates for slots and refund tiers.
/// </summary>
internal sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow { get; } = utcNow;
}
