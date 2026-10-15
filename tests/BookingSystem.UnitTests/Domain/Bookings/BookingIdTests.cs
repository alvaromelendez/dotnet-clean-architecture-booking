using BookingSystem.Domain.Bookings;

namespace BookingSystem.UnitTests.Domain.Bookings;

public sealed class BookingIdTests
{
    [Fact]
    public void New_WhenCalled_ReturnsUniqueNonEmptyId()
    {
        var first = BookingId.New();
        var second = BookingId.New();

        Assert.NotEqual(Guid.Empty, first.Value);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => BookingId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithSameGuid_ReturnsEqualIds()
    {
        var guid = Guid.NewGuid();

        Assert.Equal(BookingId.From(guid), BookingId.From(guid));
    }
}
