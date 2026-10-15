using FluentValidation;

namespace BookingSystem.Application.Rooms.ActivateRoom;

internal sealed class ActivateRoomCommandValidator : AbstractValidator<ActivateRoomCommand>
{
    public ActivateRoomCommandValidator()
    {
        RuleFor(command => command.RoomId).NotEmpty();
    }
}
