using FluentValidation;

namespace BookingSystem.Application.Bookings.GetBookingById;

internal sealed class GetBookingByIdQueryValidator : AbstractValidator<GetBookingByIdQuery>
{
    public GetBookingByIdQueryValidator()
    {
        RuleFor(query => query.BookingId).NotEmpty();
    }
}
