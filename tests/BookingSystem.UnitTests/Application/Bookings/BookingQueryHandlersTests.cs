using BookingSystem.Application.Abstractions.Paging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Application.Bookings;
using BookingSystem.Application.Bookings.GetBookingById;
using BookingSystem.Application.Bookings.GetUserBookings;
using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Bookings;
using NSubstitute;
using Shouldly;

namespace BookingSystem.UnitTests.Application.Bookings;

public sealed class BookingQueryHandlersTests
{
    private readonly IBookingQueries _bookingQueries = Substitute.For<IBookingQueries>();

    [Fact]
    public async Task GetBookingById_WhenBookingDoesNotExist_ReturnsNotFound()
    {
        var bookingId = Guid.NewGuid();
        _bookingQueries.GetByIdAsync(bookingId, Arg.Any<CancellationToken>()).Returns((BookingDto?)null);
        var handler = new GetBookingByIdQueryHandler(_bookingQueries);

        var result = await handler.Handle(new GetBookingByIdQuery(bookingId), CancellationToken.None);

        result.Error.ShouldBe(BookingApplicationErrors.NotFound(bookingId));
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetBookingById_WhenBookingExists_ReturnsDto()
    {
        var dto = SampleDto();
        _bookingQueries.GetByIdAsync(dto.Id, Arg.Any<CancellationToken>()).Returns(dto);
        var handler = new GetBookingByIdQueryHandler(_bookingQueries);

        var result = await handler.Handle(new GetBookingByIdQuery(dto.Id), CancellationToken.None);

        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task GetUserBookings_WithFilters_ForwardsThemAndReturnsPage()
    {
        var userId = Guid.NewGuid();
        var page = new PagedResult<BookingDto>([SampleDto()], Page: 2, PageSize: 10, TotalCount: 11);
        _bookingQueries
            .GetByUserAsync(userId, BookingStatus.Confirmed, 2, 10, Arg.Any<CancellationToken>())
            .Returns(page);
        var handler = new GetUserBookingsQueryHandler(_bookingQueries);

        var result = await handler.Handle(
            new GetUserBookingsQuery(userId, BookingStatus.Confirmed, Page: 2, PageSize: 10), CancellationToken.None);

        result.Value.ShouldBeSameAs(page);
        result.Value.TotalPages.ShouldBe(2);
        result.Value.HasPreviousPage.ShouldBeTrue();
        result.Value.HasNextPage.ShouldBeFalse();
    }

    private static BookingDto SampleDto() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 3, 4, 12, 0, 0, DateTimeKind.Utc),
        Attendees: 3,
        TotalPrice: 40m,
        Currency: "USD",
        BookingStatus.Pending,
        CreatedAtUtc: new DateTime(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc),
        ConfirmedAtUtc: null,
        CancelledAtUtc: null,
        RefundAmount: null);
}
