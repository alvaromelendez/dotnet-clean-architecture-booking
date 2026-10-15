namespace BookingSystem.Domain.Rooms;

/// <summary>
/// Strongly typed identifier of a <see cref="Room"/>.
/// </summary>
public readonly record struct RoomId
{
    private RoomId(Guid value) => Value = value;

    public Guid Value { get; }

    public static RoomId New() => new(Guid.NewGuid());

    /// <exception cref="ArgumentException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static RoomId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("A room identifier cannot be empty.", nameof(value))
            : new RoomId(value);

    public override string ToString() => Value.ToString();
}
