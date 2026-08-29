using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultDeliveryAddressLine1",
                table: "AspNetUsers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultDeliveryAddressLine2",
                table: "AspNetUsers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultDeliveryCity",
                table: "AspNetUsers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultDeliveryContactName",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultDeliveryCountry",
                table: "AspNetUsers",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultDeliveryPhone",
                table: "AspNetUsers",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultDeliveryPostalCode",
                table: "AspNetUsers",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultDeliveryAddressLine1",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultDeliveryAddressLine2",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultDeliveryCity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultDeliveryContactName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultDeliveryCountry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultDeliveryPhone",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultDeliveryPostalCode",
                table: "AspNetUsers");
        }
    }
}
