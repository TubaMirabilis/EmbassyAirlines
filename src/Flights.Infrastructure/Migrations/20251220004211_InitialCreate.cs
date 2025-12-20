using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Flights.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "flights");

        migrationBuilder.CreateTable(
            name: "aircraft",
            schema: "flights",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                tail_number = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                equipment_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_aircraft", x => x.id));

        migrationBuilder.CreateTable(
            name: "airports",
            schema: "flights",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                iata_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                icao_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_airports", x => x.id));

        migrationBuilder.CreateTable(
            name: "flights",
            schema: "flights",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                flight_number_iata = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                flight_number_icao = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                status = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                departure_local_time = table.Column<LocalDateTime>(type: "timestamp without time zone", nullable: false),
                arrival_local_time = table.Column<LocalDateTime>(type: "timestamp without time zone", nullable: false),
                scheduling_ambiguity_policy = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                aircraft_id = table.Column<Guid>(type: "uuid", nullable: false),
                arrival_airport_id = table.Column<Guid>(type: "uuid", nullable: false),
                departure_airport_id = table.Column<Guid>(type: "uuid", nullable: false),
                business_price_amount = table.Column<decimal>(type: "numeric", nullable: false),
                economy_price_amount = table.Column<decimal>(type: "numeric", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_flights", x => x.id);
                table.ForeignKey(
                    name: "fk_flights_aircraft_aircraft_id",
                    column: x => x.aircraft_id,
                    principalSchema: "flights",
                    principalTable: "aircraft",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_flights_airports_arrival_airport_id",
                    column: x => x.arrival_airport_id,
                    principalSchema: "flights",
                    principalTable: "airports",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_flights_airports_departure_airport_id",
                    column: x => x.departure_airport_id,
                    principalSchema: "flights",
                    principalTable: "airports",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_flights_aircraft_id",
            schema: "flights",
            table: "flights",
            column: "aircraft_id");

        migrationBuilder.CreateIndex(
            name: "ix_flights_arrival_airport_id",
            schema: "flights",
            table: "flights",
            column: "arrival_airport_id");

        migrationBuilder.CreateIndex(
            name: "ix_flights_departure_airport_id",
            schema: "flights",
            table: "flights",
            column: "departure_airport_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "flights",
            schema: "flights");

        migrationBuilder.DropTable(
            name: "aircraft",
            schema: "flights");

        migrationBuilder.DropTable(
            name: "airports",
            schema: "flights");
    }
}
