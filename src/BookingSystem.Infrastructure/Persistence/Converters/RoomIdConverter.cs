using BookingSystem.Domain.Rooms;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BookingSystem.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores <see cref="RoomId"/> as a plain <c>uuid</c> column.
/// </summary>
internal sealed class RoomIdConverter()
    : ValueConverter<RoomId, Guid>(id => id.Value, value => RoomId.From(value));
