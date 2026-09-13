using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PrintTemplateCultures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PrintTemplates_DocumentKind",
                table: "PrintTemplates");

            migrationBuilder.DropIndex(
                name: "IX_PrintTemplates_Code",
                table: "PrintTemplates");

            migrationBuilder.AddColumn<string>(
                name: "Culture",
                table: "PrintTemplates",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "pl-PL");

            migrationBuilder.CreateIndex(
                name: "IX_PrintTemplates_Code_Culture",
                table: "PrintTemplates",
                columns: new[] { "Code", "Culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrintTemplates_DocumentKind_Culture",
                table: "PrintTemplates",
                columns: new[] { "DocumentKind", "Culture" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PrintTemplates_DocumentKind_Culture",
                table: "PrintTemplates");

            migrationBuilder.DropIndex(
                name: "IX_PrintTemplates_Code_Culture",
                table: "PrintTemplates");

            migrationBuilder.DropColumn(
                name: "Culture",
                table: "PrintTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_PrintTemplates_DocumentKind",
                table: "PrintTemplates",
                column: "DocumentKind");

            migrationBuilder.CreateIndex(
                name: "IX_PrintTemplates_Code",
                table: "PrintTemplates",
                column: "Code",
                unique: true);
        }
    }
}
