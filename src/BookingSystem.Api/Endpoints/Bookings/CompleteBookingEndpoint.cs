using BookingSystem.Api.Infrastructure;
using BookingSystem.Application.Bookings.CompleteBooking;
using MediatR;

namespace BookingSystem.Api.Endpoints.Bookings;

internal sealed class CompleteBookingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("bookings/{id:guid}/complete", HandleAsync)
            .WithTags(EndpointTags.Bookings)
            .WithSummary("Complete a booking")
            .WithDescription("Confirmed → Completed, once the slot has ended. Returns 409 otherwise.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CompleteBookingCommand(id), cancellationToken);

        return result.Match(Results.NoContent, ResultExtensions.ToProblem);
    }
}
