namespace BookingSystem.Domain.Abstractions;

/// <summary>
/// Describes an expected business failure.
/// </summary>
/// <remarks>
/// The record is not sealed so outer layers can add specialized errors
/// (for example, a validation error carrying per-property messages).
/// </remarks>
/// <param name="Code">Stable, machine-readable code formatted as <c>Source.Reason</c> (e.g. <c>Booking.RoomInactive</c>).</param>
/// <param name="Description">Human-readable explanation of the failure.</param>
/// <param name="Type">Category of the failure.</param>
public record Error(string Code, string Description, ErrorType Type)
{
    /// <summary>
    /// Represents the absence of an error. Only successful results carry it.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);
}
