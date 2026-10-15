using BookingSystem.Application.Rooms;

namespace BookingSystem.Api.Contracts.Rooms;

internal static class RoomContractMappings
{
    public static RoomResponse ToResponse(this RoomDto room) =>
        new(room.Id, room.Name, room.Capacity, room.HourlyRate, room.Currency, room.IsActive);

    public static IReadOnlyList<RoomResponse> ToResponse(this IReadOnlyList<RoomDto> rooms) =>
        rooms.Select(ToResponse).ToList();
}
