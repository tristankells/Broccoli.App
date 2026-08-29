using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Broccoli.Avalonia.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonalityBandThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "SeasonalityGreenAlwaysHigh",
                table: "MacroTargetSettings",
                type: "REAL",
                nullable: false,
                defaultValue: 80.0);

            migrationBuilder.AddColumn<double>(
                name: "SeasonalityGreenFloor",
                table: "MacroTargetSettings",
                type: "REAL",
                nullable: false,
                defaultValue: 55.0);

            migrationBuilder.AddColumn<double>(
                name: "SeasonalityRedAlwaysLow",
                table: "MacroTargetSettings",
                type: "REAL",
                nullable: false,
                defaultValue: 25.0);

            migrationBuilder.AddColumn<double>(
                name: "SeasonalityRedCeiling",
                table: "MacroTargetSettings",
                type: "REAL",
                nullable: false,
                defaultValue: 45.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeasonalityGreenAlwaysHigh",
                table: "MacroTargetSettings");

            migrationBuilder.DropColumn(
                name: "SeasonalityGreenFloor",
                table: "MacroTargetSettings");

            migrationBuilder.DropColumn(
                name: "SeasonalityRedAlwaysLow",
                table: "MacroTargetSettings");

            migrationBuilder.DropColumn(
                name: "SeasonalityRedCeiling",
                table: "MacroTargetSettings");
        }
    }
}
