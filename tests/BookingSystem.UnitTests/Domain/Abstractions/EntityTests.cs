using BookingSystem.Domain.Abstractions;

namespace BookingSystem.UnitTests.Domain.Abstractions;

public sealed class EntityTests
{
    [Fact]
    public void Constructor_WithDefaultId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SampleEntity(default));
    }

    [Fact]
    public void Equals_SameTypeAndSameId_ReturnsTrue()
    {
        var id = SampleId.New();

        var first = new SampleEntity(id);
        var second = new SampleEntity(id);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_SameTypeAndDifferentId_ReturnsFalse()
    {
        var first = new SampleEntity(SampleId.New());
        var second = new SampleEntity(SampleId.New());

        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void Equals_DifferentTypeAndSameId_ReturnsFalse()
    {
        var id = SampleId.New();

        Entity<SampleId> first = new SampleEntity(id);
        Entity<SampleId> second = new OtherSampleEntity(id);

        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Equals_SameInstance_ReturnsTrue()
    {
        var entity = new SampleEntity(SampleId.New());

        Assert.True(entity.Equals(entity));
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var entity = new SampleEntity(SampleId.New());

        Assert.False(entity.Equals((Entity<SampleId>?)null));
        Assert.False(entity.Equals((object?)null));
        Assert.False(entity == null);
        Assert.True(entity != null);
    }

    [Fact]
    public void Equals_WithObjectThatIsNotAnEntity_ReturnsFalse()
    {
        var entity = new SampleEntity(SampleId.New());

        Assert.False(entity.Equals(entity.Id));
    }

    [Fact]
    public void Raise_WithNullEvent_ThrowsArgumentNullException()
    {
        var entity = new SampleEntity(SampleId.New());

        Assert.Throws<ArgumentNullException>(() => entity.RaiseEvent(null!));
    }

    [Fact]
    public void Raise_WithDomainEvent_AddsItToDomainEvents()
    {
        var entity = new SampleEntity(SampleId.New());
        var domainEvent = new SampleDomainEvent();

        entity.RaiseEvent(domainEvent);

        Assert.Same(domainEvent, Assert.Single(entity.DomainEvents));
    }

    [Fact]
    public void ClearDomainEvents_WithRaisedEvents_RemovesAllEvents()
    {
        var entity = new SampleEntity(SampleId.New());
        entity.RaiseEvent(new SampleDomainEvent());

        entity.ClearDomainEvents();

        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void DomainEvents_WhenExposed_CannotBeMutatedFromOutside()
    {
        var entity = new SampleEntity(SampleId.New());

        Assert.IsNotType<List<IDomainEvent>>(entity.DomainEvents);
    }

    private readonly record struct SampleId(Guid Value)
    {
        public static SampleId New() => new(Guid.NewGuid());
    }

    private sealed record SampleDomainEvent : IDomainEvent;

    private sealed class SampleEntity(SampleId id) : Entity<SampleId>(id)
    {
        public void RaiseEvent(IDomainEvent domainEvent) => Raise(domainEvent);
    }

    private sealed class OtherSampleEntity(SampleId id) : Entity<SampleId>(id);
}
