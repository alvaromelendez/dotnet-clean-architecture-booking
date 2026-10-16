using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Application.Rooms;
using BookingSystem.Application.Rooms.ActivateRoom;
using BookingSystem.Application.Rooms.DeactivateRoom;
using BookingSystem.Domain.Rooms;
using NSubstitute;
using Shouldly;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Application.Rooms;

public sealed class RoomActivationCommandHandlersTests
{
    private readonly IRoomRepository _roomRepository = Substitute.For<IRoomRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Room _room = new RoomBuilder().Build();

    public RoomActivationCommandHandlersTests()
    {
        _roomRepository.GetByIdAsync(_room.Id, Arg.Any<CancellationToken>()).Returns(_room);
    }

    [Fact]
    public async Task DeactivateRoom_WhenActive_DeactivatesAndSavesChanges()
    {
        var handler = new DeactivateRoomCommandHandler(_roomRepository, _unitOfWork);

        var result = await handler.Handle(new DeactivateRoomCommand(_room.Id.Value), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _room.IsActive.ShouldBeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateRoom_WhenAlreadyInactive_ReturnsConflictAndDoesNotSave()
    {
        _room.Deactivate();
        var handler = new DeactivateRoomCommandHandler(_roomRepository, _unitOfWork);

        var result = await handler.Handle(new DeactivateRoomCommand(_room.Id.Value), CancellationToken.None);

        result.Error.ShouldBe(RoomErrors.AlreadyInactive);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task DeactivateRoom_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        var roomId = Guid.NewGuid();
        var handler = new DeactivateRoomCommandHandler(_roomRepository, _unitOfWork);

        var result = await handler.Handle(new DeactivateRoomCommand(roomId), CancellationToken.None);

        result.Error.ShouldBe(RoomApplicationErrors.NotFound(roomId));
    }

    [Fact]
    public async Task ActivateRoom_WhenInactive_ActivatesAndSavesChanges()
    {
        _room.Deactivate();
        var handler = new ActivateRoomCommandHandler(_roomRepository, _unitOfWork);

        var result = await handler.Handle(new ActivateRoomCommand(_room.Id.Value), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _room.IsActive.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateRoom_WhenRoomDoesNotExist_ReturnsNotFoundAndDoesNotSave()
    {
        var roomId = Guid.NewGuid();
        var handler = new ActivateRoomCommandHandler(_roomRepository, _unitOfWork);

        var result = await handler.Handle(new ActivateRoomCommand(roomId), CancellationToken.None);

        result.Error.ShouldBe(RoomApplicationErrors.NotFound(roomId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ActivateRoom_WhenAlreadyActive_ReturnsConflictAndDoesNotSave()
    {
        var handler = new ActivateRoomCommandHandler(_roomRepository, _unitOfWork);

        var result = await handler.Handle(new ActivateRoomCommand(_room.Id.Value), CancellationToken.None);

        result.Error.ShouldBe(RoomErrors.AlreadyActive);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
