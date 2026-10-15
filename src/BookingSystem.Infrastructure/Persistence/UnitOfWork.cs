using System.Collections.Concurrent;
using System.Reflection;
using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BookingSystem.Infrastructure.Persistence;

/// <summary>
/// Commits tracked changes, translates persistence conflicts and dispatches domain events.
/// </summary>
/// <remarks>
/// This is a separate class instead of having <see cref="ApplicationDbContext"/> implement
/// <see cref="IUnitOfWork"/>: the context stays a pure mapping type (also used by migrations tooling
/// and read-side queries), and event dispatch plus exception translation stay explicit instead of
/// hiding behind a <c>SaveChanges</c> override that every caller would trigger.
/// </remarks>
internal sealed partial class UnitOfWork(
    ApplicationDbContext dbContext,
    IPublisher publisher,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private static readonly ConcurrentDictionary<Type, Type> NotificationTypes = new();
    private static readonly ConcurrentDictionary<Type, AggregateAccessor?> AggregateAccessors = new();

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // a. Collect and clear events before saving, so they are dispatched at most once.
        var domainEvents = DequeueDomainEvents();

        // b. Persist, translating provider-specific conflicts into an application-level exception.
        int affectedRows;
        try
        {
            affectedRows = await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "The data was modified by another operation after it was loaded.", exception);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
        {
            throw new ConcurrencyConflictException(
                "The change conflicts with data written concurrently by another operation.", exception);
        }

        // c. Publish only after the commit succeeded.
        // Delivery is at-most-once: if the process stops here, or a handler fails, the events are lost
        // even though the data is committed. The robust solution is the Transactional Outbox pattern
        // (store events in the same transaction, publish them from a background worker), which is
        // out of scope for this project.
        await PublishAsync(domainEvents, cancellationToken);

        return affectedRows;
    }

    private List<IDomainEvent> DequeueDomainEvents()
    {
        var domainEvents = new List<IDomainEvent>();

        foreach (var entry in dbContext.ChangeTracker.Entries())
        {
            var accessor = AggregateAccessors.GetOrAdd(entry.Metadata.ClrType, AggregateAccessor.TryCreate);
            if (accessor is null)
            {
                continue;
            }

            domainEvents.AddRange(accessor.GetDomainEvents(entry.Entity));
            accessor.ClearDomainEvents(entry.Entity);
        }

        return domainEvents;
    }

    private async Task PublishAsync(List<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            var notification = CreateNotification(domainEvent);

            try
            {
                await publisher.Publish(notification, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The transaction is already committed: failing the request now would report an
                // error for an operation that succeeded. Log it and keep dispatching the rest.
                LogDispatchFailed(logger, exception, domainEvent.GetType().Name);
            }
        }
    }

    private static object CreateNotification(IDomainEvent domainEvent)
    {
        var notificationType = NotificationTypes.GetOrAdd(
            domainEvent.GetType(),
            eventType => typeof(DomainEventNotification<>).MakeGenericType(eventType));

        return Activator.CreateInstance(notificationType, domainEvent)!;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to dispatch domain event {DomainEventType}")]
    private static partial void LogDispatchFailed(ILogger logger, Exception exception, string domainEventType);

    /// <summary>
    /// Reflection-based access to the domain events of any <see cref="AggregateRoot{TId}"/>,
    /// whatever its id type. Built once per CLR type; <c>null</c> for non-aggregates (e.g. owned types).
    /// </summary>
    private sealed class AggregateAccessor(PropertyInfo domainEventsProperty, MethodInfo clearMethod)
    {
        public static AggregateAccessor? TryCreate(Type clrType)
        {
            for (var type = clrType; type is not null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
                {
                    return new AggregateAccessor(
                        type.GetProperty(nameof(AggregateRoot<Guid>.DomainEvents))!,
                        type.GetMethod(nameof(AggregateRoot<Guid>.ClearDomainEvents))!);
                }
            }

            return null;
        }

        public IReadOnlyCollection<IDomainEvent> GetDomainEvents(object aggregate) =>
            (IReadOnlyCollection<IDomainEvent>)domainEventsProperty.GetValue(aggregate)!;

        public void ClearDomainEvents(object aggregate) => clearMethod.Invoke(aggregate, null);
    }
}
