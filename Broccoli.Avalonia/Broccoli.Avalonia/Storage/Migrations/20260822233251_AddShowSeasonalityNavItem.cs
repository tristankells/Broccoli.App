using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Broccoli.Avalonia.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddShowSeasonalityNavItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowSeasonalityNavItem",
                table: "MacroTargetSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowSeasonalityNavItem",
                table: "MacroTargetSettings");
        }
    }
}
