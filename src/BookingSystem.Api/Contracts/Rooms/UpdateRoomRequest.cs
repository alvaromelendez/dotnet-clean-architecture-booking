namespace BookingSystem.Api.Contracts.Rooms;

/// <param name="Currency">Three-letter ISO 4217 code, e.g. <c>USD</c>.</param>
public sealed record UpdateRoomRequest(string Name, int Capacity, decimal HourlyRate, string Currency);
