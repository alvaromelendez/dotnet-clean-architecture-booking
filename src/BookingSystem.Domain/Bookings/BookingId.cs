namespace BookingSystem.Domain.Bookings;

/// <summary>
/// Strongly typed identifier of a <see cref="Booking"/>.
/// </summary>
public readonly record struct BookingId
{
    private BookingId(Guid value) => Value = value;

    public Guid Value { get; }

    public static BookingId New() => new(Guid.NewGuid());

    /// <exception cref="ArgumentException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static BookingId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("A booking identifier cannot be empty.", nameof(value))
            : new BookingId(value);

    public override string ToString() => Value.ToString();
}
