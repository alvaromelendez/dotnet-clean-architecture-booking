using BookingSystem.Api.Contracts.Common;
using BookingSystem.Api.Contracts.Rooms;
using BookingSystem.Api.Infrastructure;
using BookingSystem.Application.Rooms.CreateRoom;
using MediatR;

namespace BookingSystem.Api.Endpoints.Rooms;

internal sealed class CreateRoomEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("rooms", HandleAsync)
            .WithTags(EndpointTags.Rooms)
            .WithSummary("Create a room")
            .WithDescription("Creates a bookable space. Names are unique regardless of case.")
            .Produces<ResourceCreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        CreateRoomRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateRoomCommand(request.Name, request.Capacity, request.HourlyRate, request.Currency);

        var result = await sender.Send(command, cancellationToken);

        return result.Match(
            roomId => Results.CreatedAtRoute(
                GetRoomByIdEndpoint.Name,
                new { id = roomId },
                new ResourceCreatedResponse(roomId)),
            ResultExtensions.ToProblem);
    }
}
