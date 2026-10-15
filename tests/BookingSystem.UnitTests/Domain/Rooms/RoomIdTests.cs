using BookingSystem.Domain.Rooms;

namespace BookingSystem.UnitTests.Domain.Rooms;

public sealed class RoomIdTests
{
    [Fact]
    public void New_WhenCalled_ReturnsUniqueNonEmptyId()
    {
        var first = RoomId.New();
        var second = RoomId.New();

        Assert.NotEqual(Guid.Empty, first.Value);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => RoomId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithSameGuid_ReturnsEqualIds()
    {
        var guid = Guid.NewGuid();

        Assert.Equal(RoomId.From(guid), RoomId.From(guid));
    }
}
