using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EmbassyAirlines.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Aircraft",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Registration = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Model = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    EconomySeats = table.Column<int>(type: "integer", nullable: false),
                    BusinessSeats = table.Column<int>(type: "integer", nullable: false),
                    FlightHours = table.Column<float>(type: "real", nullable: false),
                    BasicEmptyWeight = table.Column<int>(type: "integer", nullable: false),
                    MaximumZeroFuelWeight = table.Column<int>(type: "integer", nullable: false),
                    MaximumTakeoffWeight = table.Column<int>(type: "integer", nullable: false),
                    MaximumLandingWeight = table.Column<int>(type: "integer", nullable: false),
                    MaximumCargoWeight = table.Column<int>(type: "integer", nullable: false),
                    FuelOnboard = table.Column<int>(type: "integer", nullable: false),
                    FuelCapacity = table.Column<int>(type: "integer", nullable: false),
                    MinimumCabinCrew = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aircraft", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Aircraft");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");
        }
    }
}
