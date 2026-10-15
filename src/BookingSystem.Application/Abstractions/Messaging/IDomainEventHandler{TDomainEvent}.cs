using BookingSystem.Domain.Abstractions;
using MediatR;

namespace BookingSystem.Application.Abstractions.Messaging;

/// <summary>
/// Reacts to a domain event published through <see cref="DomainEventNotification{TDomainEvent}"/>.
/// </summary>
public interface IDomainEventHandler<TDomainEvent> : INotificationHandler<DomainEventNotification<TDomainEvent>>
    where TDomainEvent : IDomainEvent;
