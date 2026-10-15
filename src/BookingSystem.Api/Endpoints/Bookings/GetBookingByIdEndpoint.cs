using BookingSystem.Api.Contracts.Bookings;
using BookingSystem.Api.Infrastructure;
using BookingSystem.Application.Bookings.GetBookingById;
using MediatR;

namespace BookingSystem.Api.Endpoints.Bookings;

internal sealed class GetBookingByIdEndpoint : IEndpoint
{
    public const string Name = "GetBookingById";

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("bookings/{id:guid}", HandleAsync)
            .WithName(Name)
            .WithTags(EndpointTags.Bookings)
            .WithSummary("Get a booking")
            .WithDescription("Returns a booking by its id.")
            .Produces<BookingResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBookingByIdQuery(id), cancellationToken);

        return result.Match(booking => Results.Ok(booking.ToResponse()), ResultExtensions.ToProblem);
    }
}
