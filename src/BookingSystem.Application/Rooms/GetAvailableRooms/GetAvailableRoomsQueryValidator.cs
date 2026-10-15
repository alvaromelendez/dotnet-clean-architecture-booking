using FluentValidation;

namespace BookingSystem.Application.Rooms.GetAvailableRooms;

internal sealed class GetAvailableRoomsQueryValidator : AbstractValidator<GetAvailableRoomsQuery>
{
    public GetAvailableRoomsQueryValidator()
    {
        RuleFor(query => query.StartUtc).NotEmpty();
        RuleFor(query => query.EndUtc).NotEmpty();
        RuleFor(query => query.MinCapacity).GreaterThanOrEqualTo(1);
    }
}
