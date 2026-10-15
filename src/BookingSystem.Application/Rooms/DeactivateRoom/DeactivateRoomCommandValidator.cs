using FluentValidation;

namespace BookingSystem.Application.Rooms.DeactivateRoom;

internal sealed class DeactivateRoomCommandValidator : AbstractValidator<DeactivateRoomCommand>
{
    public DeactivateRoomCommandValidator()
    {
        RuleFor(command => command.RoomId).NotEmpty();
    }
}
