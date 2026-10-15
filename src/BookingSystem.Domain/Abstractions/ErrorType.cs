namespace BookingSystem.Domain.Abstractions;

/// <summary>
/// Category of an <see cref="Error"/>, used by outer layers to translate failures
/// (for example into HTTP status codes) without inspecting error codes.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Generic failure that does not fit another category.
    /// </summary>
    Failure = 0,

    /// <summary>
    /// The input is malformed or breaks a rule about its own values.
    /// </summary>
    Validation = 1,

    /// <summary>
    /// A referenced resource does not exist.
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// The operation conflicts with the current state of the system.
    /// </summary>
    Conflict = 3,
}
