namespace BookingSystem.Api.Endpoints;

/// <summary>
/// A single HTTP endpoint. Implementations are discovered by reflection and mapped onto a
/// versioned route group, so adding an endpoint never requires touching <c>Program.cs</c>.
/// </summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
