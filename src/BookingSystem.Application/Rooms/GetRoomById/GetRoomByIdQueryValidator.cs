using FluentValidation;

namespace BookingSystem.Application.Rooms.GetRoomById;

internal sealed class GetRoomByIdQueryValidator : AbstractValidator<GetRoomByIdQuery>
{
    public GetRoomByIdQueryValidator()
    {
        RuleFor(query => query.RoomId).NotEmpty();
    }
}
