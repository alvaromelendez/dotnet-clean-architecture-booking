using BookingSystem.Api.Infrastructure;
using BookingSystem.Application.Rooms.ActivateRoom;
using MediatR;

namespace BookingSystem.Api.Endpoints.Rooms;

internal sealed class ActivateRoomEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("rooms/{id:guid}/activate", HandleAsync)
            .WithTags(EndpointTags.Rooms)
            .WithSummary("Activate a room")
            .WithDescription("Makes an inactive room bookable again. Fails with 409 if it is already active.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ActivateRoomCommand(id), cancellationToken);

        return result.Match(Results.NoContent, ResultExtensions.ToProblem);
    }
}
