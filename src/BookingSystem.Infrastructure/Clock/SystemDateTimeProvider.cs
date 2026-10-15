using BookingSystem.Application.Abstractions.Clock;

namespace BookingSystem.Infrastructure.Clock;

/// <summary>
/// The only place in the solution that reads the system clock.
/// </summary>
internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
