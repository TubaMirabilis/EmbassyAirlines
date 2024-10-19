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
                available_seats_business = table.Column<int>(type: "integer", nullable: false),
                available_seats_economy = table.Column<int>(type: "integer", nullable: false),
                pricing_business_price = table.Column<decimal>(type: "numeric", nullable: false),
                pricing_economy_price = table.Column<decimal>(type: "numeric", nullable: false),
                schedule_arrival_time = table.Column<ZonedDateTime>(type: "timestamp with time zone", nullable: false),
                schedule_departure_time = table.Column<ZonedDateTime>(type: "timestamp with time zone", nullable: false),
                schedule_departure_airport_iata_code = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                schedule_departure_airport_time_zone = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                schedule_destination_airport_iata_code = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                schedule_destination_airport_time_zone = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_flights", x => x.id));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "flights");
    }
}
