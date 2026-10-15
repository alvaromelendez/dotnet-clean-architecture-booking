namespace BookingSystem.Api.Contracts.Common;

/// <summary>
/// Body of a <c>201 Created</c> response. The full resource is available at the <c>Location</c> header.
/// </summary>
public sealed record ResourceCreatedResponse(Guid Id);
