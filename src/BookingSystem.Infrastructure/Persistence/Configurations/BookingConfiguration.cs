using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Infrastructure.Persistence.Configurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    private const int StatusMaxLength = 20;

    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(booking => booking.Id);
        builder.Property(booking => booking.Id).ValueGeneratedNever();

        // Aggregates reference each other by id only: no navigation property, but a real FK for integrity.
        // The (room_id, status) index below also serves as the FK index.
        builder.Property(booking => booking.RoomId).IsRequired();
        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(booking => booking.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(booking => booking.UserId).IsRequired();

        // Column names are fixed explicitly because the exclusion constraint in the migration depends on them.
        builder.OwnsOne(booking => booking.TimeSlot, slot =>
        {
            slot.Property(value => value.Start).HasColumnName("start_utc").HasColumnType("timestamptz");
            slot.Property(value => value.End).HasColumnName("end_utc").HasColumnType("timestamptz");
        });
        builder.Navigation(booking => booking.TimeSlot).IsRequired();

        builder.Property(booking => booking.Attendees);

        builder.OwnsOne(booking => booking.TotalPrice, money => money.MapMoney("total_price"));
        builder.Navigation(booking => booking.TotalPrice).IsRequired();

        builder.OwnsOne(booking => booking.RefundAmount, money => money.MapMoney("refund"));
        builder.Navigation(booking => booking.RefundAmount).IsRequired(false);

        // Stored as text so the data stays readable and the exclusion constraint can filter by name.
        builder.Property(booking => booking.Status)
            .HasConversion<string>()
            .HasMaxLength(StatusMaxLength);

        builder.Property(booking => booking.CreatedAtUtc);
        builder.Property(booking => booking.ConfirmedAtUtc);
        builder.Property(booking => booking.CancelledAtUtc);

        builder.Ignore(booking => booking.IsActive);
        builder.Ignore(booking => booking.DomainEvents);

        builder.HasIndex(booking => new { booking.RoomId, booking.Status });
        builder.HasIndex(booking => new { booking.UserId, booking.CreatedAtUtc });

        builder.HasXminRowVersion();
    }
}
