using BookingSystem.Domain.Abstractions;
using MediatR;

namespace BookingSystem.Application.Abstractions.Messaging;

/// <summary>
/// A request that changes state and returns no value.
/// </summary>
public interface ICommand : IRequest<Result>;
