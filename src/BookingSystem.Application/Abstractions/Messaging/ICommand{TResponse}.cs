using BookingSystem.Domain.Abstractions;
using MediatR;

namespace BookingSystem.Application.Abstractions.Messaging;

/// <summary>
/// A request that changes state and returns a <typeparamref name="TResponse"/>.
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
