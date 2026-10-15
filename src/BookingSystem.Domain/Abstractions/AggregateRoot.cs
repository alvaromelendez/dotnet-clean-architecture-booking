namespace BookingSystem.Domain.Abstractions;

/// <summary>
/// Entry point of a consistency boundary. Outer layers load, persist and reference
/// aggregates only through their root.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : struct, IEquatable<TId>
{
    /// <inheritdoc cref="Entity{TId}()"/>
    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id)
        : base(id)
    {
    }
}
