using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MenuItemIngredientCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuItemIngredients_MenuItemId_IngredientId",
                table: "MenuItemIngredients");

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraIngredientPrice",
                table: "RestaurantSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CustomizationsJson",
                table: "OrderItems",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraCharge",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "EnableIngredientSwap",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "MenuItemIngredients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubstitute",
                table: "MenuItemIngredients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemIngredients_MenuItemId_IngredientId_IsDefault_IsSubstitute",
                table: "MenuItemIngredients",
                columns: new[] { "MenuItemId", "IngredientId", "IsDefault", "IsSubstitute" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuItemIngredients_MenuItemId_IngredientId_IsDefault_IsSubstitute",
                table: "MenuItemIngredients");

            migrationBuilder.DropColumn(
                name: "ExtraIngredientPrice",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "CustomizationsJson",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ExtraCharge",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "EnableIngredientSwap",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "MenuItemIngredients");

            migrationBuilder.DropColumn(
                name: "IsSubstitute",
                table: "MenuItemIngredients");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemIngredients_MenuItemId_IngredientId",
                table: "MenuItemIngredients",
                columns: new[] { "MenuItemId", "IngredientId" },
                unique: true);
        }
    }
}
