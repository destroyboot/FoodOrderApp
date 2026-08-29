using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260516190000_PushDeviceRegistrations")]
    public partial class PushDeviceRegistrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PushDeviceRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExpoPushToken = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastRegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSuccessfulPushAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastErrorAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushDeviceRegistrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PushDeviceRegistrations_OwnerKey_ExpoPushToken",
                table: "PushDeviceRegistrations",
                columns: new[] { "OwnerKey", "ExpoPushToken" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushDeviceRegistrations_OwnerKey_IsActive_LastSeenAt",
                table: "PushDeviceRegistrations",
                columns: new[] { "OwnerKey", "IsActive", "LastSeenAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PushDeviceRegistrations");
        }
    }
}
