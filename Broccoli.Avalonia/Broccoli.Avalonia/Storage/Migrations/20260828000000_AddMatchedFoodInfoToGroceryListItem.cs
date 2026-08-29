using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Broccoli.Avalonia.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchedFoodInfoToGroceryListItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MatchedFoodInfo",
                table: "GroceryListItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchedFoodInfo",
                table: "GroceryListItems");
        }
    }
}
