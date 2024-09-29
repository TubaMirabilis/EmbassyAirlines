using System;
using Microsoft.EntityFrameworkCore.Migrations;

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
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                flight_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                departure = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                destination = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                departure_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                arrival_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                price = table.Column<decimal>(type: "numeric", nullable: false),
                available_seats = table.Column<int>(type: "integer", nullable: false)
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
