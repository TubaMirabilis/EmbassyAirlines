using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Airports.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "airports");

        migrationBuilder.CreateTable(
            name: "airports",
            schema: "airports",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                icao_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                iata_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_airports", x => x.id));

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            schema: "airports",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(256)", unicode: false, maxLength: 256, nullable: false),
                content = table.Column<string>(type: "text", nullable: false),
                created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                error = table.Column<string>(type: "text", nullable: true),
                retry_count = table.Column<int>(type: "integer", nullable: false),
                next_attempt_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                dead_lettered_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_outbox_messages", x => x.id));

        migrationBuilder.CreateIndex(
            name: "ix_outbox_messages_unprocessed",
            schema: "airports",
            table: "outbox_messages",
            column: "created_on_utc",
            filter: "processed_on_utc IS NULL AND dead_lettered_on_utc IS NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "airports",
            schema: "airports");

        migrationBuilder.DropTable(
            name: "outbox_messages",
            schema: "airports");
    }
}
