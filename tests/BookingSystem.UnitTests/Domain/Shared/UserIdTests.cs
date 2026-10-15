using BookingSystem.Domain.Shared;

namespace BookingSystem.UnitTests.Domain.Shared;

public sealed class UserIdTests
{
    [Fact]
    public void New_WhenCalled_ReturnsUniqueNonEmptyId()
    {
        var first = UserId.New();
        var second = UserId.New();

        Assert.NotEqual(Guid.Empty, first.Value);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => UserId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithSameGuid_ReturnsEqualIds()
    {
        var guid = Guid.NewGuid();

        Assert.Equal(UserId.From(guid), UserId.From(guid));
    }
}
