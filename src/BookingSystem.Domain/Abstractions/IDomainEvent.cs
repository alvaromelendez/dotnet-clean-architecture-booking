namespace BookingSystem.Domain.Abstractions;

/// <summary>
/// Marker for something meaningful that happened in the domain.
/// Domain events are raised by entities and dispatched by outer layers.
/// </summary>
public interface IDomainEvent;
