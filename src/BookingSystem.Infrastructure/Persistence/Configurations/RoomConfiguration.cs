using BookingSystem.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Infrastructure.Persistence.Configurations;

internal sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");

        builder.HasKey(room => room.Id);
        builder.Property(room => room.Id).ValueGeneratedNever();

        // Case-insensitive uniqueness is enforced by a unique index on lower(name), created in the
        // initial migration because the Fluent API cannot express expression indexes.
        builder.Property(room => room.Name)
            .HasMaxLength(Room.MaxNameLength)
            .IsRequired();

        builder.Property(room => room.Capacity);
        builder.Property(room => room.IsActive);

        builder.OwnsOne(room => room.HourlyRate, money => money.MapMoney("hourly_rate"));
        builder.Navigation(room => room.HourlyRate).IsRequired();

        builder.Ignore(room => room.DomainEvents);

        builder.HasXminRowVersion();
    }
}
