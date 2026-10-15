namespace BookingSystem.Api.Contracts.Rooms;

public sealed record RoomResponse(
    Guid Id,
    string Name,
    int Capacity,
    decimal HourlyRate,
    string Currency,
    bool IsActive);
