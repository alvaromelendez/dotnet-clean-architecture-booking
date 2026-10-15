using BookingSystem.Api.Contracts.Rooms;
using BookingSystem.Api.Infrastructure;
using BookingSystem.Application.Rooms.GetRoomById;
using MediatR;

namespace BookingSystem.Api.Endpoints.Rooms;

internal sealed class GetRoomByIdEndpoint : IEndpoint
{
    public const string Name = "GetRoomById";

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("rooms/{id:guid}", HandleAsync)
            .WithName(Name)
            .WithTags(EndpointTags.Rooms)
            .WithSummary("Get a room")
            .WithDescription("Returns a room by its id.")
            .Produces<RoomResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRoomByIdQuery(id), cancellationToken);

        return result.Match(room => Results.Ok(room.ToResponse()), ResultExtensions.ToProblem);
    }
}
