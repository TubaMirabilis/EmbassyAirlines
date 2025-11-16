using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aircraft.Api.Lambda.Migrations;

/// <inheritdoc />
internal partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "aircraft",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                tail_number = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                equipment_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                dry_operating_weight_kilograms = table.Column<int>(type: "integer", nullable: false),
                maximum_fuel_weight_kilograms = table.Column<int>(type: "integer", nullable: false),
                maximum_landing_weight_kilograms = table.Column<int>(type: "integer", nullable: false),
                maximum_takeoff_weight_kilograms = table.Column<int>(type: "integer", nullable: false),
                maximum_zero_fuel_weight_kilograms = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_aircraft", x => x.id));

        migrationBuilder.CreateTable(
            name: "seats",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                aircraft_id = table.Column<Guid>(type: "uuid", nullable: false),
                row_number = table.Column<byte>(type: "smallint", nullable: false),
                letter = table.Column<char>(type: "character(1)", nullable: false),
                type = table.Column<string>(type: "character varying(24)", unicode: false, maxLength: 24, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_seats", x => x.id);
                table.ForeignKey(
                    name: "fk_seats_aircraft_aircraft_id",
                    column: x => x.aircraft_id,
                    principalTable: "aircraft",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_seats_aircraft_id",
            table: "seats",
            column: "aircraft_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "seats");

        migrationBuilder.DropTable(
            name: "aircraft");
    }
}
