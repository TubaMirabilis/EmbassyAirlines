using System;
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
            name: "airports",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                iata_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_airports", x => x.id));

        migrationBuilder.CreateTable(
            name: "itineraries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                reference = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: false),
                lead_passenger_email = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_itineraries", x => x.id));

        migrationBuilder.CreateTable(
            name: "flights",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                flight_number = table.Column<string>(type: "character varying(6)", unicode: false, maxLength: 6, nullable: false),
                departure_local_time = table.Column<LocalDateTime>(type: "timestamp without time zone", nullable: false),
                arrival_local_time = table.Column<LocalDateTime>(type: "timestamp without time zone", nullable: false),
                departure_airport_id = table.Column<Guid>(type: "uuid", nullable: false),
                arrival_airport_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_flights", x => x.id);
                table.ForeignKey(
                    name: "fk_flights_airports_arrival_airport_id",
                    column: x => x.arrival_airport_id,
                    principalTable: "airports",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_flights_airports_departure_airport_id",
                    column: x => x.departure_airport_id,
                    principalTable: "airports",
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
                is_cancelled = table.Column<bool>(type: "boolean", nullable: false),
                flight_id = table.Column<Guid>(type: "uuid", nullable: false),
                itinerary_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_bookings", x => x.id);
                table.ForeignKey(
                    name: "fk_bookings_flights_flight_id",
                    column: x => x.flight_id,
                    principalTable: "flights",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_bookings_itineraries_itinerary_id",
                    column: x => x.itinerary_id,
                    principalTable: "itineraries",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "passenger",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                booking_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_passenger", x => x.id);
                table.ForeignKey(
                    name: "fk_passenger_bookings_booking_id",
                    column: x => x.booking_id,
                    principalTable: "bookings",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "seats",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                seat_number = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                seat_type = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                price = table.Column<decimal>(type: "numeric(9,2)", nullable: false),
                flight_id = table.Column<Guid>(type: "uuid", nullable: false),
                passenger_id = table.Column<Guid>(type: "uuid", nullable: true),
                version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
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
                table.ForeignKey(
                    name: "fk_seats_passenger_passenger_id",
                    column: x => x.passenger_id,
                    principalTable: "passenger",
                    principalColumn: "id");
            });

        migrationBuilder.CreateIndex(
            name: "ix_bookings_flight_id",
            table: "bookings",
            column: "flight_id");

        migrationBuilder.CreateIndex(
            name: "ix_bookings_itinerary_id",
            table: "bookings",
            column: "itinerary_id");

        migrationBuilder.CreateIndex(
            name: "ix_flights_arrival_airport_id",
            table: "flights",
            column: "arrival_airport_id");

        migrationBuilder.CreateIndex(
            name: "ix_flights_departure_airport_id",
            table: "flights",
            column: "departure_airport_id");

        migrationBuilder.CreateIndex(
            name: "ix_passenger_booking_id",
            table: "passenger",
            column: "booking_id");

        migrationBuilder.CreateIndex(
            name: "ix_seats_flight_id_passenger_id",
            table: "seats",
            columns: ["flight_id", "passenger_id"],
            unique: true,
            filter: "passenger_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_seats_flight_id_seat_number",
            table: "seats",
            columns: ["flight_id", "seat_number"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_seats_passenger_id",
            table: "seats",
            column: "passenger_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "seats");

        migrationBuilder.DropTable(
            name: "passenger");

        migrationBuilder.DropTable(
            name: "bookings");

        migrationBuilder.DropTable(
            name: "flights");

        migrationBuilder.DropTable(
            name: "itineraries");

        migrationBuilder.DropTable(
            name: "airports");
    }
}
