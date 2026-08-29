using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestaurantSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestaurantSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    EnableTableOrders = table.Column<bool>(type: "bit", nullable: false),
                    EnableTakeawayOrders = table.Column<bool>(type: "bit", nullable: false),
                    EnableDeliveryOrders = table.Column<bool>(type: "bit", nullable: false),
                    EnablePayInApp = table.Column<bool>(type: "bit", nullable: false),
                    EnablePayAtCounter = table.Column<bool>(type: "bit", nullable: false),
                    EnableReservations = table.Column<bool>(type: "bit", nullable: false),
                    ReservationRequiresInAppPayment = table.Column<bool>(type: "bit", nullable: false),
                    ReservationPreorderMinOffsetMinutes = table.Column<int>(type: "int", nullable: false),
                    ReservationPreorderMaxAfterStartMinutes = table.Column<int>(type: "int", nullable: false),
                    DeliveryFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedPreparationBaseMinutes = table.Column<int>(type: "int", nullable: false),
                    EstimatedPreparationPerItemMinutes = table.Column<int>(type: "int", nullable: false),
                    SupportedCultures = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DefaultCulture = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantSettings_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO RestaurantSettings
                    (RestaurantId, EnableTableOrders, EnableTakeawayOrders, EnableDeliveryOrders,
                     EnablePayInApp, EnablePayAtCounter, EnableReservations, ReservationRequiresInAppPayment,
                     ReservationPreorderMinOffsetMinutes, ReservationPreorderMaxAfterStartMinutes,
                     DeliveryFee, EstimatedPreparationBaseMinutes, EstimatedPreparationPerItemMinutes,
                     SupportedCultures, DefaultCulture)
                SELECT
                    r.Id, 1, 1, 0,
                    1, 1, 0, 1,
                    5, 60,
                    8.00, 10, 2,
                    'pl-PL,en-US', 'pl-PL'
                FROM Restaurants r
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM RestaurantSettings s
                    WHERE s.RestaurantId = r.Id
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantSettings_RestaurantId",
                table: "RestaurantSettings",
                column: "RestaurantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantSettings");
        }
    }
}
