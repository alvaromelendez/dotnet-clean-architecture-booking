using BookingSystem.Api.Contracts.Bookings;
using BookingSystem.Api.Contracts.Common;
using BookingSystem.Api.Infrastructure;
using BookingSystem.Application.Bookings.GetUserBookings;
using MediatR;

namespace BookingSystem.Api.Endpoints.Bookings;

internal sealed class GetUserBookingsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("users/{userId:guid}/bookings", HandleAsync)
            .WithTags(EndpointTags.Bookings)
            .WithSummary("List a user's bookings")
            .WithDescription(
                "Pages through a user's bookings, most recently created first, optionally filtered by status. " +
                $"`pageSize` must be between 1 and {GetUserBookingsQuery.MaxPageSize}.")
            .Produces<PagedResponse<BookingResponse>>()
            .ProducesValidationProblem();

    private static async Task<IResult> HandleAsync(
        Guid userId,
        BookingStatusContract? status,
        int? page,
        int? pageSize,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetUserBookingsQuery(
            userId,
            status?.ToDomain(),
            page ?? 1,
            pageSize ?? GetUserBookingsQuery.DefaultPageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.Match(bookings => Results.Ok(bookings.ToResponse()), ResultExtensions.ToProblem);
    }
}
