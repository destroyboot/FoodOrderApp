using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserBillingProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultBillingAddressLine1",
                table: "AspNetUsers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBillingAddressLine2",
                table: "AspNetUsers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBillingCity",
                table: "AspNetUsers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBillingCompanyName",
                table: "AspNetUsers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBillingCountry",
                table: "AspNetUsers",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultBillingCustomerType",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBillingPersonName",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBillingPostalCode",
                table: "AspNetUsers",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBillingReceiptEmail",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBillingTaxId",
                table: "AspNetUsers",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultBillingAddressLine1",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultBillingAddressLine2",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultBillingCity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultBillingCompanyName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultBillingCountry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultBillingCustomerType",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultBillingPersonName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultBillingPostalCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultBillingReceiptEmail",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultBillingTaxId",
                table: "AspNetUsers");
        }
    }
}
