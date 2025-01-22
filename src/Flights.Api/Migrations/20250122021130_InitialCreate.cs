using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Flights.Api.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "flights",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                flight_number = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: false),
                schedule_arrival_time = table.Column<ZonedDateTime>(type: "timestamp with time zone", nullable: false),
                schedule_departure_time = table.Column<ZonedDateTime>(type: "timestamp with time zone", nullable: false),
                schedule_departure_airport_iata_code = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                schedule_departure_airport_time_zone = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                schedule_destination_airport_iata_code = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                schedule_destination_airport_time_zone = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_flights", x => x.id));

        migrationBuilder.CreateTable(
            name: "seats",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                seat_number = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                seat_type = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                is_booked = table.Column<bool>(type: "boolean", nullable: false),
                price = table.Column<decimal>(type: "numeric", nullable: false),
                flight_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_seats", x => x.id);
                table.ForeignKey(
                    name: "fk_seats_flights_flight_id",
                    column: x => x.flight_id,
                    principalTable: "flights",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "bookings",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                seat_id = table.Column<Guid>(type: "uuid", nullable: false),
                passenger_name = table.Column<string>(type: "text", nullable: false),
                passenger_email = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_bookings", x => x.id);
                table.ForeignKey(
                    name: "fk_bookings_seats_seat_id",
                    column: x => x.seat_id,
                    principalTable: "seats",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_bookings_seat_id",
            table: "bookings",
            column: "seat_id");

        migrationBuilder.CreateIndex(
            name: "ix_seats_flight_id",
            table: "seats",
            column: "flight_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "bookings");

        migrationBuilder.DropTable(
            name: "seats");

        migrationBuilder.DropTable(
            name: "flights");
    }
}
