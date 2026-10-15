using BookingSystem.Domain.Bookings;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BookingSystem.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores <see cref="BookingId"/> as a plain <c>uuid</c> column.
/// </summary>
internal sealed class BookingIdConverter()
    : ValueConverter<BookingId, Guid>(id => id.Value, value => BookingId.From(value));
