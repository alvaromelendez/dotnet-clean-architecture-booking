using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Application.Rooms;
using BookingSystem.Application.Rooms.GetAvailableRooms;
using BookingSystem.Application.Rooms.GetRoomById;
using BookingSystem.Application.Rooms.ListRooms;
using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Shared;
using NSubstitute;
using Shouldly;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Application.Rooms;

public sealed class RoomQueryHandlersTests
{
    private static readonly RoomDto SampleRoom = new(Guid.NewGuid(), "Court 1", 4, 15m, "USD", IsActive: true);

    private readonly IRoomQueries _roomQueries = Substitute.For<IRoomQueries>();

    [Fact]
    public async Task GetRoomById_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        var roomId = Guid.NewGuid();
        _roomQueries.GetByIdAsync(roomId, Arg.Any<CancellationToken>()).Returns((RoomDto?)null);
        var handler = new GetRoomByIdQueryHandler(_roomQueries);

        var result = await handler.Handle(new GetRoomByIdQuery(roomId), CancellationToken.None);

        result.Error.ShouldBe(RoomApplicationErrors.NotFound(roomId));
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetRoomById_WhenRoomExists_ReturnsDto()
    {
        _roomQueries.GetByIdAsync(SampleRoom.Id, Arg.Any<CancellationToken>()).Returns(SampleRoom);
        var handler = new GetRoomByIdQueryHandler(_roomQueries);

        var result = await handler.Handle(new GetRoomByIdQuery(SampleRoom.Id), CancellationToken.None);

        result.Value.ShouldBe(SampleRoom);
    }

    [Fact]
    public async Task ListRooms_WithActiveFilter_ForwardsFilterAndReturnsRooms()
    {
        _roomQueries.ListAsync(true, Arg.Any<CancellationToken>()).Returns([SampleRoom]);
        var handler = new ListRoomsQueryHandler(_roomQueries);

        var result = await handler.Handle(new ListRoomsQuery(IsActive: true), CancellationToken.None);

        result.Value.ShouldHaveSingleItem().ShouldBe(SampleRoom);
    }

    [Fact]
    public async Task GetAvailableRooms_WithValidSlot_QueriesBySlotAndCapacity()
    {
        var slot = SlotStartingIn(TimeSpan.FromDays(1));
        _roomQueries.GetAvailableAsync(slot, 3, Arg.Any<CancellationToken>()).Returns([SampleRoom]);
        var handler = new GetAvailableRoomsQueryHandler(_roomQueries);

        var result = await handler.Handle(new GetAvailableRoomsQuery(slot.Start, slot.End, 3), CancellationToken.None);

        result.Value.ShouldHaveSingleItem().ShouldBe(SampleRoom);
    }

    [Fact]
    public async Task GetAvailableRooms_WithInvalidSlot_PropagatesTimeSlotErrorWithoutQuerying()
    {
        var start = UtcNow.AddDays(1);
        var handler = new GetAvailableRoomsQueryHandler(_roomQueries);

        var result = await handler.Handle(new GetAvailableRoomsQuery(start, start.AddMinutes(10)), CancellationToken.None);

        result.Error.ShouldBe(TimeSlotErrors.TooShort);
        await _roomQueries.DidNotReceiveWithAnyArgs().GetAvailableAsync(default!, default, default);
    }
}
