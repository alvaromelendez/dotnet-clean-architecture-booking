using BookingSystem.Application.Abstractions.Clock;
using BookingSystem.UnitTests.Domain;

namespace BookingSystem.UnitTests.Application.Fakes;

/// <summary>
/// Deterministic clock. Defaults to <see cref="TestData.UtcNow"/> so it lines up with the domain test builders.
/// </summary>
internal sealed class FakeDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    public FakeDateTimeProvider()
        : this(TestData.UtcNow)
    {
    }

    public DateTime UtcNow { get; private set; } = utcNow;

    public void SetUtcNow(DateTime utcNow) => UtcNow = utcNow;

    public void Advance(TimeSpan duration) => UtcNow += duration;
}
