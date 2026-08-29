using Microsoft.EntityFrameworkCore.Migrations;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260704173200_RemoveSpanishLocalization")]
    public partial class RemoveSpanishLocalization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM [AppTextTranslations] WHERE [Culture] = N'es-ES';
                DELETE FROM [AppLanguages] WHERE [Culture] = N'es-ES';

                UPDATE [RestaurantSettings]
                SET [SupportedCultures] = REPLACE(REPLACE([SupportedCultures], N',es-ES', N''), N'es-ES,', N''),
                    [DefaultCulture] = CASE WHEN [DefaultCulture] = N'es-ES' THEN N'pl-PL' ELSE [DefaultCulture] END;

                UPDATE [AspNetUsers]
                SET [PreferredCulture] = NULL
                WHERE [PreferredCulture] = N'es-ES';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [AppLanguages] WHERE [Culture] = N'es-ES')
                BEGIN
                    INSERT INTO [AppLanguages] ([Culture], [DisplayName], [NativeName], [IsActive], [IsDefault], [SortOrder])
                    VALUES (N'es-ES', N'Spanish', N'Espanol', 1, 0, 3);
                END;
                """);
        }
    }
}
