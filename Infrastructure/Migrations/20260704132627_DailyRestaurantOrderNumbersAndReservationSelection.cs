using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DailyRestaurantOrderNumbersAndReservationSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowUserTableSelectionForReservations",
                table: "RestaurantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DailyRestaurantOrderDate",
                table: "Orders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DailyRestaurantOrderNumber",
                table: "Orders",
                type: "int",
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowUserTableSelectionForReservations",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "DailyRestaurantOrderDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DailyRestaurantOrderNumber",
                table: "Orders");
        }
    }
}
