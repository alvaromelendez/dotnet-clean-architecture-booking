namespace BookingSystem.Domain.Abstractions;

/// <summary>
/// Describes an expected business failure.
/// </summary>
/// <param name="Code">Stable, machine-readable code formatted as <c>Source.Reason</c> (e.g. <c>Booking.RoomInactive</c>).</param>
/// <param name="Description">Human-readable explanation of the failure.</param>
public sealed record Error(string Code, string Description)
{
    /// <summary>
    /// Represents the absence of an error. Only successful results carry it.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);
}
