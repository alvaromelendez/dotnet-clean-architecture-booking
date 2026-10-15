namespace BookingSystem.Domain.Abstractions;

/// <summary>
/// Base class for objects defined by their identity rather than by their attributes.
/// Two entities are equal when they have the same concrete type and the same <see cref="Id"/>.
/// </summary>
/// <typeparam name="TId">Strongly typed identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Reserved for object-relational mappers that materialize entities from storage
    /// and assign <see cref="Id"/> afterwards. Domain code must use <see cref="Entity{TId}(TId)"/>.
    /// </summary>
    protected Entity()
    {
    }

    protected Entity(TId id)
    {
        if (id.Equals(default))
        {
            throw new ArgumentException("An entity identifier cannot be the default value.", nameof(id));
        }

        Id = id;
    }

    public TId Id { get; private set; }

    /// <summary>
    /// Domain events raised by this entity that have not been dispatched yet.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other.GetType() == GetType() && Id.Equals(other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
