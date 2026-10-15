using BookingSystem.Api.Contracts.Rooms;
using BookingSystem.Api.Infrastructure;
using BookingSystem.Application.Rooms.UpdateRoomDetails;
using MediatR;

namespace BookingSystem.Api.Endpoints.Rooms;

internal sealed class UpdateRoomDetailsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("rooms/{id:guid}", HandleAsync)
            .WithTags(EndpointTags.Rooms)
            .WithSummary("Update a room")
            .WithDescription("Replaces the name, capacity and hourly rate of a room.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateRoomRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoomDetailsCommand(id, request.Name, request.Capacity, request.HourlyRate, request.Currency);

        var result = await sender.Send(command, cancellationToken);

        return result.Match(Results.NoContent, ResultExtensions.ToProblem);
    }
}
