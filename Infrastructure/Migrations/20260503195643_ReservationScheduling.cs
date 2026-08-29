using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReservationScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_RestaurantTableId",
                table: "Reservations");

            migrationBuilder.AddColumn<bool>(
                name: "IsReservable",
                table: "RestaurantTables",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultReservationDurationMinutes",
                table: "RestaurantSettings",
                type: "int",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.AddColumn<int>(
                name: "ReservationGracePeriodMinutes",
                table: "RestaurantSettings",
                type: "int",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.AddColumn<bool>(
                name: "ReservationHoldsTableUntilClose",
                table: "RestaurantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReservationLastStartMinuteOfDay",
                table: "RestaurantSettings",
                type: "int",
                nullable: false,
                defaultValue: 1380);

            migrationBuilder.AddColumn<int>(
                name: "ReservationStartMinuteOfDay",
                table: "RestaurantSettings",
                type: "int",
                nullable: false,
                defaultValue: 1020);

            migrationBuilder.Sql("UPDATE RestaurantTables SET IsReservable = 1 WHERE IsReservable = 0");
            migrationBuilder.Sql("UPDATE RestaurantSettings SET DefaultReservationDurationMinutes = 90 WHERE DefaultReservationDurationMinutes = 0");
            migrationBuilder.Sql("UPDATE RestaurantSettings SET ReservationGracePeriodMinutes = 15 WHERE ReservationGracePeriodMinutes = 0");
            migrationBuilder.Sql("UPDATE RestaurantSettings SET ReservationStartMinuteOfDay = 1020 WHERE ReservationStartMinuteOfDay = 0");
            migrationBuilder.Sql("UPDATE RestaurantSettings SET ReservationLastStartMinuteOfDay = 1380 WHERE ReservationLastStartMinuteOfDay = 0");

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleasedAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RestaurantTableId_StartAt_EndAt_Status",
                table: "Reservations",
                columns: new[] { "RestaurantTableId", "StartAt", "EndAt", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_RestaurantTableId_StartAt_EndAt_Status",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "IsReservable",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "DefaultReservationDurationMinutes",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "ReservationGracePeriodMinutes",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "ReservationHoldsTableUntilClose",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "ReservationLastStartMinuteOfDay",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "ReservationStartMinuteOfDay",
                table: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RestaurantTableId",
                table: "Reservations",
                column: "RestaurantTableId");
        }
    }
}
