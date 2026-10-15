using System.Runtime.CompilerServices;

namespace BookingSystem.Domain.Abstractions;

/// <summary>
/// Guards against non-UTC timestamps. The domain never reads the system clock:
/// the current time is always passed in, and it must be UTC.
/// </summary>
internal static class DateTimeGuard
{
    public static void EnsureUtc(
        DateTime value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The value must be expressed in UTC (DateTimeKind.Utc).", paramName);
        }
    }
}
