using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Application.Rooms;
using BookingSystem.Application.Rooms.CreateRoom;
using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;
using NSubstitute;
using Shouldly;

namespace BookingSystem.UnitTests.Application.Rooms;

public sealed class CreateRoomCommandHandlerTests
{
    private readonly IRoomRepository _roomRepository = Substitute.For<IRoomRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateRoomCommandHandler _handler;

    public CreateRoomCommandHandlerTests()
    {
        _handler = new CreateRoomCommandHandler(_roomRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithUniqueName_AddsRoomSavesChangesAndReturnsId()
    {
        var result = await _handler.Handle(new CreateRoomCommand("Court 1", 4, 15m, "usd"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _roomRepository.Received(1).AddAsync(
            Arg.Is<Room>(room =>
                room.Id.Value == result.Value &&
                room.Name == "Court 1" &&
                room.HourlyRate == Money.Create(15m, "USD").Value),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ReturnsConflictAndDoesNotPersist()
    {
        _roomRepository.ExistsByNameAsync("Court 1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new CreateRoomCommand("  Court 1 ", 4, 15m, "USD"), CancellationToken.None);

        result.Error.ShouldBe(RoomApplicationErrors.NameNotUnique("Court 1"));
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        await _roomRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenDomainRejectsRoom_PropagatesErrorWithoutCheckingName()
    {
        var result = await _handler.Handle(new CreateRoomCommand("Court 1", 0, 15m, "USD"), CancellationToken.None);

        result.Error.ShouldBe(RoomErrors.InvalidCapacity);
        await _roomRepository.DidNotReceiveWithAnyArgs().ExistsByNameAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WithNegativeHourlyRate_PropagatesMoneyError()
    {
        var result = await _handler.Handle(new CreateRoomCommand("Court 1", 4, -1m, "USD"), CancellationToken.None);

        result.Error.ShouldBe(MoneyErrors.NegativeAmount);
    }
}
