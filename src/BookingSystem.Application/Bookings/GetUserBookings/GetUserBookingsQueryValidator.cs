using FluentValidation;

namespace BookingSystem.Application.Bookings.GetUserBookings;

internal sealed class GetUserBookingsQueryValidator : AbstractValidator<GetUserBookingsQuery>
{
    public GetUserBookingsQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
        RuleFor(query => query.Status).IsInEnum();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, GetUserBookingsQuery.MaxPageSize);
    }
}
