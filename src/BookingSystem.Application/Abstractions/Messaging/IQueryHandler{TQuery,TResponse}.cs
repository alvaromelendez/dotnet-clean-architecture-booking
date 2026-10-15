using BookingSystem.Domain.Abstractions;
using MediatR;

namespace BookingSystem.Application.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
