using BookingSystem.Application.Behaviors;
using BookingSystem.Application.Rooms.CreateRoom;
using BookingSystem.Domain.Abstractions;
using BookingSystem.UnitTests.Application.Fakes;
using MediatR;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace BookingSystem.UnitTests.Application.Behaviors;

public sealed class LoggingBehaviorTests
{
    private const string SensitiveName = "Dr. Jane Doe private office";

    private static readonly CreateRoomCommand Command = new(SensitiveName, 2, 50m, "USD");

    private readonly ListLogger<LoggingBehavior<CreateRoomCommand, Result<Guid>>> _logger = new();
    private readonly LoggingBehavior<CreateRoomCommand, Result<Guid>> _behavior;

    public LoggingBehaviorTests()
    {
        _behavior = new LoggingBehavior<CreateRoomCommand, Result<Guid>>(_logger);
    }

    [Fact]
    public async Task Handle_WhenRequestSucceeds_LogsStartAndCompletionWithRequestName()
    {
        var expected = Result.Success(Guid.NewGuid());

        var response = await _behavior.Handle(Command, _ => Task.FromResult(expected), CancellationToken.None);

        response.ShouldBeSameAs(expected);
        _logger.Entries.Count.ShouldBe(2);
        _logger.Entries.ShouldAllBe(entry => entry.Level == LogLevel.Information);
        _logger.Entries.ShouldAllBe(entry => entry.Message.Contains(nameof(CreateRoomCommand)));
        _logger.Entries[1].Message.ShouldContain(" ms");
    }

    [Fact]
    public async Task Handle_WhenRequestFails_LogsWarningWithErrorCode()
    {
        var error = Error.Conflict("Room.NameNotUnique", "Duplicate.");

        await _behavior.Handle(
            Command, _ => Task.FromResult(Result.Failure<Guid>(error)), CancellationToken.None);

        var (level, message) = _logger.Entries[^1];
        level.ShouldBe(LogLevel.Warning);
        message.ShouldContain("Room.NameNotUnique");
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_LogsErrorAndRethrows()
    {
        RequestHandlerDelegate<Result<Guid>> next = _ => throw new InvalidOperationException("boom");

        await Should.ThrowAsync<InvalidOperationException>(() => _behavior.Handle(Command, next, CancellationToken.None));

        _logger.Entries[^1].Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task Handle_Always_DoesNotLogRequestContent()
    {
        await _behavior.Handle(
            Command, _ => Task.FromResult(Result.Success(Guid.NewGuid())), CancellationToken.None);

        _logger.Entries.ShouldNotContain(entry => entry.Message.Contains(SensitiveName));
    }
}
