using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Application.Abstractions.Validation;

/// <summary>
/// Validation failure of a request's input, with the messages grouped by property name.
/// </summary>
public sealed record ValidationError : Error
{
    public const string ErrorCode = "Validation.Failed";

    public ValidationError(IReadOnlyDictionary<string, string[]> errors)
        : base(ErrorCode, "One or more validation errors occurred.", ErrorType.Validation)
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors;
    }

    /// <summary>
    /// Error messages keyed by the name of the invalid property.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
