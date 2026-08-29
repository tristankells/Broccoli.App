using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Broccoli.Avalonia.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonalityWeightingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "SeasonalityBottomPercentile",
                table: "MacroTargetSettings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.33);

            migrationBuilder.AddColumn<int>(
                name: "SeasonalityInfluenceMode",
                table: "MacroTargetSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<double>(
                name: "SeasonalityTopPercentile",
                table: "MacroTargetSettings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.67);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeasonalityBottomPercentile",
                table: "MacroTargetSettings");

            migrationBuilder.DropColumn(
                name: "SeasonalityInfluenceMode",
                table: "MacroTargetSettings");

            migrationBuilder.DropColumn(
                name: "SeasonalityTopPercentile",
                table: "MacroTargetSettings");
        }
    }
}
