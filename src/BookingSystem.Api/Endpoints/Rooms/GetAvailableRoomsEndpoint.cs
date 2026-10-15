using BookingSystem.Api.Contracts.Rooms;
using BookingSystem.Api.Infrastructure;
using BookingSystem.Application.Rooms.GetAvailableRooms;
using MediatR;

namespace BookingSystem.Api.Endpoints.Rooms;

internal sealed class GetAvailableRoomsEndpoint : IEndpoint
{
    // The literal segment "available" always wins over "rooms/{id:guid}", which also
    // could never match it thanks to the guid constraint.
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("rooms/available", HandleAsync)
            .WithTags(EndpointTags.Rooms)
            .WithSummary("Find available rooms")
            .WithDescription(
                "Active rooms that fit at least `minCapacity` attendees and have no pending or confirmed " +
                "booking overlapping [start, end). Dates are ISO 8601 with offset; URL-encode '+' as %2B.")
            .Produces<IReadOnlyList<RoomResponse>>()
            .ProducesValidationProblem();

    private static async Task<IResult> HandleAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        int? minCapacity,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Clients may send any offset; the domain's TimeSlot only accepts DateTimeKind.Utc,
        // and DateTimeOffset.UtcDateTime is the lossless conversion that produces it.
        var query = new GetAvailableRoomsQuery(start.UtcDateTime, end.UtcDateTime, minCapacity ?? 1);

        var result = await sender.Send(query, cancellationToken);

        return result.Match(rooms => Results.Ok(rooms.ToResponse()), ResultExtensions.ToProblem);
    }
}
