using BookingSystem.Domain.Abstractions;
using MediatR;

namespace BookingSystem.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;
