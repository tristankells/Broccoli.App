using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Broccoli.Avalonia.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddProduceItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProduceItems");
        }
    }
}
