using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Broccoli.Avalonia.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddCardDisplaySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowCardImage",
                table: "MacroTargetSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowCardNutrition",
                table: "MacroTargetSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowCardSeasonality",
                table: "MacroTargetSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowCardTags",
                table: "MacroTargetSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowCardImage",
                table: "MacroTargetSettings");

            migrationBuilder.DropColumn(
                name: "ShowCardNutrition",
                table: "MacroTargetSettings");

            migrationBuilder.DropColumn(
                name: "ShowCardSeasonality",
                table: "MacroTargetSettings");

            migrationBuilder.DropColumn(
                name: "ShowCardTags",
                table: "MacroTargetSettings");
        }
    }
}
