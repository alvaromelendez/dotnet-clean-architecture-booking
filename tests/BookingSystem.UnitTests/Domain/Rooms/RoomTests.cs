using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Rooms.Events;
using BookingSystem.Domain.Shared;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Domain.Rooms;

public sealed class RoomTests
{
    [Fact]
    public void Create_WithValidData_ReturnsActiveRoom()
    {
        var result = Room.Create("Meeting Room A", 8, Usd(30m));

        Assert.True(result.IsSuccess);
        var room = result.Value;
        Assert.Equal("Meeting Room A", room.Name);
        Assert.Equal(8, room.Capacity);
        Assert.Equal(Usd(30m), room.HourlyRate);
        Assert.True(room.IsActive);
        Assert.NotEqual(Guid.Empty, room.Id.Value);
    }

    [Fact]
    public void Create_WithSurroundingWhitespaceInName_TrimsName()
    {
        var room = Room.Create("  Court 1  ", 4, Usd(10m)).Value;

        Assert.Equal("Court 1", room.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingName_ReturnsNameRequiredError(string? name)
    {
        var result = Room.Create(name!, 8, Usd(30m));

        Assert.Equal(RoomErrors.NameRequired, result.Error);
    }

    [Fact]
    public void Create_WithNameAtMaximumLength_ReturnsRoom()
    {
        var result = Room.Create(new string('a', Room.MaxNameLength), 8, Usd(30m));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WithNameAboveMaximumLength_ReturnsNameTooLongError()
    {
        var result = Room.Create(new string('a', Room.MaxNameLength + 1), 8, Usd(30m));

        Assert.Equal(RoomErrors.NameTooLong, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveCapacity_ReturnsInvalidCapacityError(int capacity)
    {
        var result = Room.Create("Meeting Room A", capacity, Usd(30m));

        Assert.Equal(RoomErrors.InvalidCapacity, result.Error);
    }

    [Fact]
    public void Create_WithNullHourlyRate_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Room.Create("Meeting Room A", 8, null!));
    }

    [Fact]
    public void UpdateDetails_WithValidData_UpdatesRoom()
    {
        var room = new RoomBuilder().Build();

        var result = room.UpdateDetails("Consulting Office 3", 2, Usd(45m));

        Assert.True(result.IsSuccess);
        Assert.Equal("Consulting Office 3", room.Name);
        Assert.Equal(2, room.Capacity);
        Assert.Equal(Usd(45m), room.HourlyRate);
    }

    [Fact]
    public void UpdateDetails_WithInvalidData_ReturnsErrorAndKeepsPreviousDetails()
    {
        var room = new RoomBuilder().WithCapacity(10).WithHourlyRate(20m).Build();

        var result = room.UpdateDetails("New Name", 0, Usd(99m));

        Assert.Equal(RoomErrors.InvalidCapacity, result.Error);
        Assert.Equal("Meeting Room A", room.Name);
        Assert.Equal(10, room.Capacity);
        Assert.Equal(Usd(20m), room.HourlyRate);
    }

    [Fact]
    public void UpdateDetails_WithNullHourlyRate_ThrowsArgumentNullExceptionAndKeepsPreviousDetails()
    {
        var room = new RoomBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => room.UpdateDetails("New Name", 4, null!));
        Assert.Equal(RoomBuilder.DefaultName, room.Name);
        Assert.Equal(Usd(RoomBuilder.DefaultHourlyRate), room.HourlyRate);
    }

    [Fact]
    public void Deactivate_WhenActive_DeactivatesAndRaisesRoomDeactivatedEvent()
    {
        var room = new RoomBuilder().Build();

        var result = room.Deactivate();

        Assert.True(result.IsSuccess);
        Assert.False(room.IsActive);
        var domainEvent = Assert.IsType<RoomDeactivatedDomainEvent>(Assert.Single(room.DomainEvents));
        Assert.Equal(room.Id, domainEvent.RoomId);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ReturnsAlreadyInactiveErrorWithoutRaisingEvent()
    {
        var room = new RoomBuilder().Build();
        room.Deactivate();
        room.ClearDomainEvents();

        var result = room.Deactivate();

        Assert.Equal(RoomErrors.AlreadyInactive, result.Error);
        Assert.Empty(room.DomainEvents);
    }

    [Fact]
    public void Activate_WhenInactive_ActivatesRoom()
    {
        var room = new RoomBuilder().Build();
        room.Deactivate();

        var result = room.Activate();

        Assert.True(result.IsSuccess);
        Assert.True(room.IsActive);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ReturnsAlreadyActiveError()
    {
        var room = new RoomBuilder().Build();

        var result = room.Activate();

        Assert.Equal(RoomErrors.AlreadyActive, result.Error);
        Assert.True(room.IsActive);
    }

    [Theory]
    [MemberData(nameof(PriceCases))]
    public void CalculatePrice_ForSlot_ReturnsHourlyRateProportionalToDuration(
        decimal hourlyRate, int durationMinutes, decimal expectedPrice)
    {
        var room = new RoomBuilder().WithHourlyRate(hourlyRate).Build();
        var slot = SlotStartingIn(TimeSpan.FromDays(1), TimeSpan.FromMinutes(durationMinutes));

        var price = room.CalculatePrice(slot);

        Assert.Equal(Usd(expectedPrice), price);
    }

    public static TheoryData<decimal, int, decimal> PriceCases => new()
    {
        { 20m, 60, 20m },
        { 20m, 30, 10m },
        { 20m, 90, 30m },
        { 15m, 45, 11.25m },
        { 10m, 50, 8.33m },
        { 25m, 480, 200m },
    };

    [Fact]
    public void CalculatePrice_WithNullSlot_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RoomBuilder().Build().CalculatePrice(null!));
    }

    [Fact]
    public void CalculatePrice_ForRoomInOtherCurrency_ReturnsPriceInRoomCurrency()
    {
        var room = Room.Create("Court 1", 4, Money.Create(12m, "EUR").Value).Value;

        var price = room.CalculatePrice(SlotStartingIn(TimeSpan.FromDays(1), TimeSpan.FromHours(1)));

        Assert.Equal("EUR", price.Currency);
    }
}
