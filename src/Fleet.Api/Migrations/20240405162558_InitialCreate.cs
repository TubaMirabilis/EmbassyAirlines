using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fleet.Api.Migrations;
/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.CreateTable(
            name: "Aircraft",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Registration = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false),
                AircraftStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                OperationalStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                Location = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                Model = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                Type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                TypeDesignator = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                Wingspan = table.Column<short>(type: "smallint", nullable: false),
                EngineModel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                EngineCount = table.Column<byte>(type: "smallint", nullable: false),
                ServiceCeiling = table.Column<int>(type: "integer", nullable: false),
                SeatingConfiguration = table.Column<string>(type: "json", nullable: false),
                FlightHours = table.Column<float>(type: "real", nullable: false),
                ProductionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                BasicEmptyWeight = table.Column<int>(type: "integer", nullable: false),
                MaximumZeroFuelWeight = table.Column<int>(type: "integer", nullable: false),
                MaximumTakeoffWeight = table.Column<int>(type: "integer", nullable: false),
                MaximumLandingWeight = table.Column<int>(type: "integer", nullable: false),
                MaximumCargoWeight = table.Column<int>(type: "integer", nullable: false),
                FuelOnboard = table.Column<int>(type: "integer", nullable: false),
                FuelCapacity = table.Column<int>(type: "integer", nullable: false),
                MinimumCabinCrew = table.Column<byte>(type: "smallint", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Aircraft", x => x.Id));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.DropTable(
            name: "Aircraft");
    }
}
