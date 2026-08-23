using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Broccoli.Avalonia.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlySeasonality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The season-based schema can't be migrated losslessly to the new per-month model,
            // so the table is rebuilt empty and re-seeded from the per-month nz-produce.json on
            // first access (ProduceSeeder.SeedIfEmpty).
            migrationBuilder.DropTable(
                name: "ProduceItems");

            migrationBuilder.CreateTable(
                name: "ProduceItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Months = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProduceItems", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProduceItems");

            migrationBuilder.CreateTable(
                name: "ProduceItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Seasons = table.Column<string>(type: "TEXT", nullable: false),
                    YearRound = table.Column<bool>(type: "INTEGER", nullable: false),
                    PeakSeasons = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProduceItems", x => x.Id);
                });
        }
    }
}
