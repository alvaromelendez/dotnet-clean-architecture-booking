using BookingSystem.Api.Contracts.Bookings;
using BookingSystem.Api.Infrastructure;
using BookingSystem.Application.Bookings.CancelBooking;
using MediatR;

namespace BookingSystem.Api.Endpoints.Bookings;

internal sealed class CancelBookingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("bookings/{id:guid}/cancel", HandleAsync)
            .WithTags(EndpointTags.Bookings)
            .WithSummary("Cancel a booking")
            .WithDescription(
                "Pending or Confirmed → Cancelled, returning the refund: 100% with 24 h or more notice, " +
                "50% with at least 2 h, otherwise 0%. Pending bookings were never charged and refund 0. " +
                "Returns 409 once the slot has started or for terminal statuses.")
            .Produces<BookingCancellationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelBookingCommand(id), cancellationToken);

        return result.Match(cancellation => Results.Ok(cancellation.ToResponse()), ResultExtensions.ToProblem);
    }
}
