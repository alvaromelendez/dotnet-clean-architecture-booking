using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.TestUtilities.Builders;

/// <summary>
/// Builds valid <see cref="Room"/>s through <see cref="Room.Create"/>. Defaults: <see cref="DefaultName"/>,
/// capacity <see cref="DefaultCapacity"/>, <see cref="DefaultHourlyRate"/> USD per hour, active.
/// The room is returned without pending domain events, as a repository would load it.
/// </summary>
public sealed class RoomBuilder
{
    public const string DefaultName = "Meeting Room A";
    public const int DefaultCapacity = 10;
    public const decimal DefaultHourlyRate = 20m;

    private string _name = DefaultName;
    private int _capacity = DefaultCapacity;
    private decimal _hourlyRate = DefaultHourlyRate;
    private string _currency = Money.DefaultCurrency;
    private bool _inactive;

    public RoomBuilder Named(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// A random name, for tests that share a database where room names must be unique.
    /// </summary>
    public RoomBuilder WithUniqueName() => Named($"Room {Guid.NewGuid():N}");

    public RoomBuilder WithCapacity(int capacity)
    {
        _capacity = capacity;
        return this;
    }

    public RoomBuilder WithHourlyRate(decimal amount, string currency = Money.DefaultCurrency)
    {
        _hourlyRate = amount;
        _currency = currency;
        return this;
    }

    public RoomBuilder Inactive()
    {
        _inactive = true;
        return this;
    }

    public Room Build()
    {
        var hourlyRate = Money.Create(_hourlyRate, _currency).ValueOrThrow("create the hourly rate");
        var room = Room.Create(_name, _capacity, hourlyRate).ValueOrThrow("create the room");

        if (_inactive)
        {
            room.Deactivate().ThrowIfFailure("deactivate the room");
        }

        room.ClearDomainEvents();

        return room;
    }
}
