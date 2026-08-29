using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestaurantDeliveryOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryAssignmentMode",
                table: "RestaurantSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryEndMinuteOfDay",
                table: "RestaurantSettings",
                type: "int",
                nullable: false,
                defaultValue: 1320);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryLeadTimeMinutes",
                table: "RestaurantSettings",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryRadiusKm",
                table: "RestaurantSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 5.00m);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryStartMinuteOfDay",
                table: "RestaurantSettings",
                type: "int",
                nullable: false,
                defaultValue: 660);

            migrationBuilder.AddColumn<bool>(
                name: "EnablePayOnDelivery",
                table: "RestaurantSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumDeliveryOrder",
                table: "RestaurantSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CuisineType",
                table: "Restaurants",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryAssignmentMode",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryEndMinuteOfDay",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryLeadTimeMinutes",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryRadiusKm",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryStartMinuteOfDay",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "EnablePayOnDelivery",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "MinimumDeliveryOrder",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "CuisineType",
                table: "Restaurants");
        }
    }
}
