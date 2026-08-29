using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var connectionString = "Server=localhost;Database=FoodOrderAppDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;";

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(connectionString)
    .Options;

await using var db = new AppDbContext(options);

var spanishLanguages = await db.AppLanguages.Where(x => x.Culture == "es-ES").ToListAsync();
if (spanishLanguages.Count > 0)
{
    db.AppLanguages.RemoveRange(spanishLanguages);
}

var spanishTexts = await db.AppTextTranslations.Where(x => x.Culture == "es-ES").ToListAsync();
if (spanishTexts.Count > 0)
{
    db.AppTextTranslations.RemoveRange(spanishTexts);
}

var restaurantSettings = await db.RestaurantSettings.ToListAsync();
foreach (var settings in restaurantSettings)
{
    var cultures = (settings.SupportedCultures ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(x => !string.Equals(x, "es-ES", StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (cultures.Count == 0)
    {
        cultures.Add("pl-PL");
    }

    settings.SupportedCultures = string.Join(",", cultures);
    if (string.Equals(settings.DefaultCulture, "es-ES", StringComparison.OrdinalIgnoreCase) || !cultures.Contains(settings.DefaultCulture, StringComparer.OrdinalIgnoreCase))
    {
        settings.DefaultCulture = cultures.Contains("pl-PL", StringComparer.OrdinalIgnoreCase) ? "pl-PL" : cultures[0];
    }
}

var spanishUsers = await db.Users.Where(x => x.PreferredCulture == "es-ES").ToListAsync();
foreach (var user in spanishUsers)
{
    user.PreferredCulture = null;
}

await db.SaveChangesAsync();

Console.WriteLine($"Removed languages: {spanishLanguages.Count}, removed texts: {spanishTexts.Count}, updated restaurant settings: {restaurantSettings.Count}, reset users: {spanishUsers.Count}");
