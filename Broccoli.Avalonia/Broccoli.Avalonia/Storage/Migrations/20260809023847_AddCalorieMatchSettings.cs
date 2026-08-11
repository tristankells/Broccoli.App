using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Broccoli.Avalonia.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddCalorieMatchSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CalorieMatchTolerancePercent",
                table: "MacroTargetSettings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "ShowCardCalorieMatch",
                table: "MacroTargetSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalorieMatchTolerancePercent",
                table: "MacroTargetSettings");

            migrationBuilder.DropColumn(
                name: "ShowCardCalorieMatch",
                table: "MacroTargetSettings");
        }
    }
}
