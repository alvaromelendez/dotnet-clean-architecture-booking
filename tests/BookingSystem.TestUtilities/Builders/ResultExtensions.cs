using BookingSystem.Domain.Abstractions;

namespace BookingSystem.TestUtilities.Builders;

internal static class ResultExtensions
{
    /// <summary>
    /// Builders only produce valid objects; a domain failure means the builder was misconfigured,
    /// so it is reported as an exception that names the rejected step and the domain error.
    /// </summary>
    public static T ValueOrThrow<T>(this Result<T> result, string step) =>
        result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException($"Builder could not {step}: {result.Error.Code} ({result.Error.Description})");

    public static void ThrowIfFailure(this Result result, string step)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Builder could not {step}: {result.Error.Code} ({result.Error.Description})");
        }
    }
}
