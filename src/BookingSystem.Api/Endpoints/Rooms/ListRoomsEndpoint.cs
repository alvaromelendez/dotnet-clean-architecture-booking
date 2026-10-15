using BookingSystem.Api.Contracts.Rooms;
using BookingSystem.Api.Infrastructure;
using BookingSystem.Application.Rooms.ListRooms;
using MediatR;

namespace BookingSystem.Api.Endpoints.Rooms;

internal sealed class ListRoomsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("rooms", HandleAsync)
            .WithTags(EndpointTags.Rooms)
            .WithSummary("List rooms")
            .WithDescription("Lists rooms ordered by name, optionally filtered by their active state.")
            .Produces<IReadOnlyList<RoomResponse>>();

    private static async Task<IResult> HandleAsync(bool? isActive, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListRoomsQuery(isActive), cancellationToken);

        return result.Match(rooms => Results.Ok(rooms.ToResponse()), ResultExtensions.ToProblem);
    }
}
