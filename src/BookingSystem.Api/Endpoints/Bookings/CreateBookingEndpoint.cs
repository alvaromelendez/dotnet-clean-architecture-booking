using BookingSystem.Api.Contracts.Bookings;
using BookingSystem.Api.Contracts.Common;
using BookingSystem.Api.Infrastructure;
using BookingSystem.Application.Bookings.CreateBooking;
using MediatR;

namespace BookingSystem.Api.Endpoints.Bookings;

internal sealed class CreateBookingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("bookings", HandleAsync)
            .WithTags(EndpointTags.Bookings)
            .WithSummary("Book a room")
            .WithDescription(
                "Creates a pending booking if the room is active, fits the attendees and is free for " +
                "[start, end). Returns 409 when the slot overlaps another booking, including when a " +
                "concurrent request claims it first.")
            .Produces<ResourceCreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        CreateBookingRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Clients may send any offset; the domain's TimeSlot only accepts DateTimeKind.Utc,
        // and DateTimeOffset.UtcDateTime is the lossless conversion that produces it.
        var command = new CreateBookingCommand(
            request.RoomId,
            request.UserId,
            request.Start.UtcDateTime,
            request.End.UtcDateTime,
            request.Attendees);

        var result = await sender.Send(command, cancellationToken);

        return result.Match(
            bookingId => Results.CreatedAtRoute(
                GetBookingByIdEndpoint.Name,
                new { id = bookingId },
                new ResourceCreatedResponse(bookingId)),
            ResultExtensions.ToProblem);
    }
}
