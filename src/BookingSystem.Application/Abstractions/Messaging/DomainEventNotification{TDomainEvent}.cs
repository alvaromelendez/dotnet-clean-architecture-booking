using BookingSystem.Domain.Abstractions;
using MediatR;

namespace BookingSystem.Application.Abstractions.Messaging;

/// <summary>
/// Adapts a domain event to a MediatR notification, so the domain stays free of MediatR.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
