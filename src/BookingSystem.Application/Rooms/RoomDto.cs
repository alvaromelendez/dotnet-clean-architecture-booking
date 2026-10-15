namespace BookingSystem.Application.Rooms;

public sealed record RoomDto(
    Guid Id,
    string Name,
    int Capacity,
    decimal HourlyRate,
    string Currency,
    bool IsActive);
