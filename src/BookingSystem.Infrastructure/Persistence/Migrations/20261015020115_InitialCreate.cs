using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        private const string RoomNameUniqueIndex = "ux_rooms_lower_name";
        private const string BookingOverlapConstraint = "ex_bookings_room_id_time_slot";
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    hourly_rate_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    hourly_rate_currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rooms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    end_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    attendees = table.Column<int>(type: "integer", nullable: false),
                    total_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_price_currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refund_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    refund_currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_bookings_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_room_id_status",
                table: "bookings",
                columns: new[] { "room_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_user_id_created_at_utc",
                table: "bookings",
                columns: new[] { "user_id", "created_at_utc" });

            // The statements below are raw SQL on purpose: EF Core's Fluent API (and Npgsql's extensions
            // to it) can model neither expression indexes nor exclusion constraints. They are static DDL
            // with no user input, so there is no injection surface.

            // Room names are unique regardless of case. RoomRepository.ExistsByNameAsync compares
            // lower(name), so this index also serves that lookup.
            migrationBuilder.Sql(
                $"CREATE UNIQUE INDEX {RoomNameUniqueIndex} ON rooms (lower(name));");

            // Last line of defense against double booking: two active (Pending/Confirmed) bookings of the
            // same room may not have overlapping half-open [start, end) ranges. The application checks
            // availability first, but only the database can close the race between concurrent requests.
            // Requires the btree_gist extension to combine uuid equality with range overlap in one GiST index.
            migrationBuilder.Sql(
                $"""
                ALTER TABLE bookings
                    ADD CONSTRAINT {BookingOverlapConstraint}
                    EXCLUDE USING gist (
                        room_id WITH =,
                        tstzrange(start_utc, end_utc, '[)') WITH &&
                    )
                    WHERE (status IN ('Pending', 'Confirmed'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"ALTER TABLE bookings DROP CONSTRAINT IF EXISTS {BookingOverlapConstraint};");

            migrationBuilder.Sql(
                $"DROP INDEX IF EXISTS {RoomNameUniqueIndex};");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "rooms");
        }
    }
}
