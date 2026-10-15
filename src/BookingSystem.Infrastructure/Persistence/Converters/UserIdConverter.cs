using BookingSystem.Domain.Shared;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BookingSystem.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores <see cref="UserId"/> as a plain <c>uuid</c> column.
/// </summary>
internal sealed class UserIdConverter()
    : ValueConverter<UserId, Guid>(id => id.Value, value => UserId.From(value));
