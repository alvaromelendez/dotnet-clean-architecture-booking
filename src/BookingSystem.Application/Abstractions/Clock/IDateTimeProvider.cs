namespace BookingSystem.Application.Abstractions.Clock;

/// <summary>
/// Source of the current time. Handlers read the clock only through this abstraction
/// and pass the value to the domain, which keeps both layers deterministic and testable.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Current instant with <see cref="DateTimeKind.Utc"/>.
    /// </summary>
    DateTime UtcNow { get; }
}
