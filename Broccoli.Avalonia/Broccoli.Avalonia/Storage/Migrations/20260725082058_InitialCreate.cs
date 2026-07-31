using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Broccoli.Avalonia.Storage.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyFoodPlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Tabs = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyFoodPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GroceryListItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsChecked = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    PartitionKey = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroceryListItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MacroTargets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    PartitionKey = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    WeightKg = table.Column<double>(type: "REAL", nullable: false),
                    HeightCm = table.Column<double>(type: "REAL", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Goal = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalCalorieDelta = table.Column<int>(type: "INTEGER", nullable: false),
                    Bmr = table.Column<double>(type: "REAL", nullable: false),
                    Tdee = table.Column<double>(type: "REAL", nullable: false),
                    RecommendedCalories = table.Column<double>(type: "REAL", nullable: false),
                    RecommendedProteinG = table.Column<double>(type: "REAL", nullable: false),
                    RecommendedCarbsG = table.Column<double>(type: "REAL", nullable: false),
                    RecommendedFatG = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacroTargets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MacroTargetSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    PartitionKey = table.Column<string>(type: "TEXT", nullable: false),
                    BmrFormula = table.Column<int>(type: "INTEGER", nullable: false),
                    ProteinMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    ProteinPercent = table.Column<double>(type: "REAL", nullable: false),
                    CarbPercent = table.Column<double>(type: "REAL", nullable: false),
                    FatPercent = table.Column<double>(type: "REAL", nullable: false),
                    ProteinGramsPerKg = table.Column<double>(type: "REAL", nullable: false),
                    UnitSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipeMealComparisonEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecipeMealComparisonPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacroTargetSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealPrepPlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    RecipeIds = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPrepPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PantryItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    PartitionKey = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PantryItems", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyFoodPlans");

            migrationBuilder.DropTable(
                name: "GroceryListItems");

            migrationBuilder.DropTable(
                name: "MacroTargets");

            migrationBuilder.DropTable(
                name: "MacroTargetSettings");

            migrationBuilder.DropTable(
                name: "MealPrepPlans");

            migrationBuilder.DropTable(
                name: "PantryItems");
        }
    }
}
