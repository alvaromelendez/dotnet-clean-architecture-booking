using BookingSystem.Api.Contracts.Common;
using BookingSystem.Application.Abstractions.Paging;
using BookingSystem.Application.Bookings;
using BookingSystem.Application.Bookings.CancelBooking;
using BookingSystem.Domain.Bookings;

namespace BookingSystem.Api.Contracts.Bookings;

internal static class BookingContractMappings
{
    public static BookingResponse ToResponse(this BookingDto booking) => new(
        booking.Id,
        booking.RoomId,
        booking.UserId,
        booking.StartUtc,
        booking.EndUtc,
        booking.Attendees,
        booking.TotalPrice,
        booking.Currency,
        (BookingStatusContract)booking.Status,
        booking.CreatedAtUtc,
        booking.ConfirmedAtUtc,
        booking.CancelledAtUtc,
        booking.RefundAmount);

    public static PagedResponse<BookingResponse> ToResponse(this PagedResult<BookingDto> page) => new(
        page.Items.Select(ToResponse).ToList(),
        page.Page,
        page.PageSize,
        page.TotalCount,
        page.TotalPages,
        page.HasPreviousPage,
        page.HasNextPage);

    public static BookingCancellationResponse ToResponse(this CancelBookingResponse cancellation) =>
        new(cancellation.BookingId, cancellation.RefundAmount, cancellation.Currency);

    /// <summary>
    /// Same numeric values by design. Undefined values (e.g. <c>?status=99</c>) pass through and are
    /// rejected with a 400 by the query validator instead of failing here.
    /// </summary>
    public static BookingStatus ToDomain(this BookingStatusContract status) => (BookingStatus)status;
}
