using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Application.Rooms;
using BookingSystem.Application.Rooms.UpdateRoomDetails;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;
using NSubstitute;
using Shouldly;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Application.Rooms;

public sealed class UpdateRoomDetailsCommandHandlerTests
{
    private readonly IRoomRepository _roomRepository = Substitute.For<IRoomRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateRoomDetailsCommandHandler _handler;
    private readonly Room _room = new RoomBuilder().Build();

    public UpdateRoomDetailsCommandHandlerTests()
    {
        _roomRepository.GetByIdAsync(_room.Id, Arg.Any<CancellationToken>()).Returns(_room);
        _handler = new UpdateRoomDetailsCommandHandler(_roomRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidDetails_UpdatesRoomAndSavesChanges()
    {
        var result = await _handler.Handle(Command("Board Room", capacity: 12), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _room.Name.ShouldBe("Board Room");
        _room.Capacity.ShouldBe(12);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRenamedToExistingName_ReturnsConflictAndDoesNotSave()
    {
        _roomRepository.ExistsByNameAsync("Board Room", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(Command("Board Room"), CancellationToken.None);

        result.Error.ShouldBe(RoomApplicationErrors.NameNotUnique("Board Room"));
        _room.Name.ShouldBe("Meeting Room A");
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenNameIsUnchangedIgnoringCase_DoesNotCheckUniqueness()
    {
        var result = await _handler.Handle(Command("MEETING ROOM A"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _roomRepository.DidNotReceiveWithAnyArgs().ExistsByNameAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WithInvalidHourlyRate_PropagatesMoneyErrorAndDoesNotSave()
    {
        var command = Command("Board Room") with { HourlyRate = -1m };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Error.ShouldBe(MoneyErrors.NegativeAmount);
        _room.Name.ShouldBe(RoomBuilder.DefaultName);
        await _roomRepository.DidNotReceiveWithAnyArgs().ExistsByNameAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenDomainRejectsDetails_PropagatesErrorKeepsRoomAndDoesNotSave()
    {
        var result = await _handler.Handle(Command("Board Room", capacity: 0), CancellationToken.None);

        result.Error.ShouldBe(RoomErrors.InvalidCapacity);
        _room.Name.ShouldBe(RoomBuilder.DefaultName);
        _room.Capacity.ShouldBe(RoomBuilder.DefaultCapacity);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        var command = Command("Board Room") with { RoomId = Guid.NewGuid() };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Error.ShouldBe(RoomApplicationErrors.NotFound(command.RoomId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private UpdateRoomDetailsCommand Command(string name, int capacity = 10) =>
        new(_room.Id.Value, name, capacity, 20m, "USD");
}
