namespace BookingSystem.Domain.Shared;

/// <summary>
/// Strongly typed identifier of the user who makes a booking.
/// Users are managed outside this bounded context; the domain only references them.
/// </summary>
public readonly record struct UserId
{
    private UserId(Guid value) => Value = value;

    public Guid Value { get; }

    public static UserId New() => new(Guid.NewGuid());

    /// <exception cref="ArgumentException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static UserId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("A user identifier cannot be empty.", nameof(value))
            : new UserId(value);

    public override string ToString() => Value.ToString();
}
