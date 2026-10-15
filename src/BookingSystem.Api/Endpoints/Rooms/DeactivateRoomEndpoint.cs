using BookingSystem.Api.Infrastructure;
using BookingSystem.Application.Rooms.DeactivateRoom;
using MediatR;

namespace BookingSystem.Api.Endpoints.Rooms;

internal sealed class DeactivateRoomEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("rooms/{id:guid}/deactivate", HandleAsync)
            .WithTags(EndpointTags.Rooms)
            .WithSummary("Deactivate a room")
            .WithDescription("Stops a room from accepting new bookings. Fails with 409 if it is already inactive.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateRoomCommand(id), cancellationToken);

        return result.Match(Results.NoContent, ResultExtensions.ToProblem);
    }
}
