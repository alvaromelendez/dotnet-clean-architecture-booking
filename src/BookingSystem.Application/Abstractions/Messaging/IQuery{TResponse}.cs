using BookingSystem.Domain.Abstractions;
using MediatR;

namespace BookingSystem.Application.Abstractions.Messaging;

/// <summary>
/// A request that reads state without changing it.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
