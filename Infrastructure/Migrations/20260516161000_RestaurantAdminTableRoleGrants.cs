using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Infrastructure.Persistence;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260516161000_RestaurantAdminTableRoleGrants")]
    public partial class RestaurantAdminTableRoleGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdminTablePermissions_UserId_TableName",
                table: "AdminTablePermissions");

            migrationBuilder.AddColumn<string>(
                name: "RoleName",
                table: "AdminTablePermissions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM AdminTablePermissions)
                BEGIN
                    WITH merged AS
                    (
                        SELECT
                            TableName,
                            MAX(CASE WHEN CanRead = 1 THEN 1 ELSE 0 END) AS CanRead,
                            MAX(CASE WHEN CanCreate = 1 THEN 1 ELSE 0 END) AS CanCreate,
                            MAX(CASE WHEN CanUpdate = 1 THEN 1 ELSE 0 END) AS CanUpdate,
                            MAX(CASE WHEN CanDelete = 1 THEN 1 ELSE 0 END) AS CanDelete
                        FROM AdminTablePermissions
                        GROUP BY TableName
                    )
                    DELETE FROM AdminTablePermissions;

                    INSERT INTO AdminTablePermissions (RoleName, TableName, CanRead, CanCreate, CanUpdate, CanDelete)
                    SELECT
                        N'RestaurantAdmin',
                        TableName,
                        CAST(CanRead AS bit),
                        CAST(CanCreate AS bit),
                        CAST(CanUpdate AS bit),
                        CAST(CanDelete AS bit)
                    FROM merged;
                END
                """);

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AdminTablePermissions");

            migrationBuilder.AlterColumn<string>(
                name: "RoleName",
                table: "AdminTablePermissions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminTablePermissions_RoleName_TableName",
                table: "AdminTablePermissions",
                columns: new[] { "RoleName", "TableName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdminTablePermissions_RoleName_TableName",
                table: "AdminTablePermissions");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "AdminTablePermissions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE AdminTablePermissions
                SET UserId = CASE
                    WHEN RoleName = N'RestaurantAdmin' THEN N'role:RestaurantAdmin'
                    ELSE N'role:' + RoleName
                END
                """);

            migrationBuilder.DropColumn(
                name: "RoleName",
                table: "AdminTablePermissions");

            migrationBuilder.CreateIndex(
                name: "IX_AdminTablePermissions_UserId_TableName",
                table: "AdminTablePermissions",
                columns: new[] { "UserId", "TableName" },
                unique: true);
        }
    }
}
