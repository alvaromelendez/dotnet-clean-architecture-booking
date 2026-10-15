using BookingSystem.Api.Infrastructure;
using BookingSystem.Application.Bookings.ConfirmBooking;
using MediatR;

namespace BookingSystem.Api.Endpoints.Bookings;

internal sealed class ConfirmBookingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("bookings/{id:guid}/confirm", HandleAsync)
            .WithTags(EndpointTags.Bookings)
            .WithSummary("Confirm a booking")
            .WithDescription("Pending → Confirmed. Returns 409 for any other current status.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmBookingCommand(id), cancellationToken);

        return result.Match(Results.NoContent, ResultExtensions.ToProblem);
    }
}
