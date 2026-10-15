using BookingSystem.Application.Abstractions.Validation;
using BookingSystem.Application.Behaviors;
using BookingSystem.Application.Rooms.CreateRoom;
using BookingSystem.Application.Rooms.DeactivateRoom;
using BookingSystem.Domain.Abstractions;
using FluentValidation;
using Shouldly;

namespace BookingSystem.UnitTests.Application.Behaviors;

public sealed class ValidationBehaviorTests
{
    private static readonly CreateRoomCommand ValidCommand = new("Meeting Room A", 8, 20m, "USD");
    private static readonly CreateRoomCommand InvalidCommand = new("", 8, 20m, "US");

    private bool _nextInvoked;

    [Fact]
    public async Task Handle_WithValidRequest_InvokesNextAndReturnsItsResponse()
    {
        var expected = Result.Success(Guid.NewGuid());
        var behavior = CreateBehavior<CreateRoomCommand, Result<Guid>>(new CreateRoomCommandValidator());

        var response = await behavior.Handle(ValidCommand, NextReturning(expected), CancellationToken.None);

        _nextInvoked.ShouldBeTrue();
        response.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ReturnsValidationErrorWithoutInvokingNext()
    {
        var behavior = CreateBehavior<CreateRoomCommand, Result<Guid>>(new CreateRoomCommandValidator());

        var response = await behavior.Handle(
            InvalidCommand, NextReturning(Result.Success(Guid.NewGuid())), CancellationToken.None);

        _nextInvoked.ShouldBeFalse();
        response.IsFailure.ShouldBeTrue();
        var error = response.Error.ShouldBeOfType<ValidationError>();
        error.Type.ShouldBe(ErrorType.Validation);
        error.Code.ShouldBe(ValidationError.ErrorCode);
        error.Errors.Keys.ShouldBe(
            [nameof(CreateRoomCommand.Name), nameof(CreateRoomCommand.Currency)],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Handle_WithInvalidRequestReturningNonGenericResult_ReturnsFailedResult()
    {
        var behavior = CreateBehavior<DeactivateRoomCommand, Result>(new DeactivateRoomCommandValidator());

        var response = await behavior.Handle(
            new DeactivateRoomCommand(Guid.Empty), NextReturning(Result.Success()), CancellationToken.None);

        _nextInvoked.ShouldBeFalse();
        response.Error.ShouldBeOfType<ValidationError>()
            .Errors.ShouldContainKey(nameof(DeactivateRoomCommand.RoomId));
    }

    [Fact]
    public async Task Handle_WithSeveralValidators_AggregatesAllErrors()
    {
        var extraValidator = new InlineValidator<CreateRoomCommand>();
        extraValidator.RuleFor(command => command.Capacity).GreaterThan(100);
        var behavior = CreateBehavior<CreateRoomCommand, Result<Guid>>(
            new CreateRoomCommandValidator(), extraValidator);

        var response = await behavior.Handle(
            InvalidCommand, NextReturning(Result.Success(Guid.NewGuid())), CancellationToken.None);

        response.Error.ShouldBeOfType<ValidationError>().Errors.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_WithoutValidators_InvokesNext()
    {
        var behavior = CreateBehavior<CreateRoomCommand, Result<Guid>>();

        await behavior.Handle(InvalidCommand, NextReturning(Result.Success(Guid.NewGuid())), CancellationToken.None);

        _nextInvoked.ShouldBeTrue();
    }

    private static ValidationBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        params IValidator<TRequest>[] validators)
        where TRequest : notnull
        where TResponse : Result =>
        new(validators);

    private MediatR.RequestHandlerDelegate<TResponse> NextReturning<TResponse>(TResponse response) =>
        _ =>
        {
            _nextInvoked = true;
            return Task.FromResult(response);
        };
}
