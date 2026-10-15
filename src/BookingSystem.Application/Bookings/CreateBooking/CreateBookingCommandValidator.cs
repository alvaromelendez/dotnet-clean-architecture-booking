using FluentValidation;

namespace BookingSystem.Application.Bookings.CreateBooking;

internal sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(command => command.RoomId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.StartUtc).NotEmpty();
        RuleFor(command => command.EndUtc).NotEmpty();
    }
}
