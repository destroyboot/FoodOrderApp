using Core.Data.Entities;
using Infrastructure.Auth;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

const string connectionString =
    "Server=localhost;Database=FoodOrderAppDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;";

var services = new ServiceCollection();

services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));
services.AddDataProtection();
services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

await using var provider = services.BuildServiceProvider();

using var scope = provider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

await db.Database.MigrateAsync();

foreach (var role in new[] { "Admin", "RestaurantAdmin", "Waiter", "Chef", "Customer" })
{
    if (!await roles.RoleExistsAsync(role))
    {
        var result = await roles.CreateAsync(new IdentityRole(role));
        ThrowIfFailed(result, $"create role {role}");
    }
}

var appAdmin = await EnsureUserAsync("app.admin@foodapp.local", "Admin123!", "Admin");
var customer = await EnsureUserAsync("customer@foodapp.local", "Customer123!", "Customer");

var bistro = await EnsureRestaurantAsync("Pierogi Bistro", "Rynek 10, Krakow");
var sushi = await EnsureRestaurantAsync("Sushi Garden", "Długa 8, Warsaw");

await EnsureRestaurantSettingsAsync(bistro, enableDeliveryOrders: false, enableReservations: true, deliveryFee: 8.00m);
await EnsureRestaurantSettingsAsync(sushi, enableDeliveryOrders: true, enableReservations: false, deliveryFee: 12.00m);

await EnsureTablesAsync(bistro, ("1", 2), ("2", 4), ("Window 3", 2), ("Patio 1", 4));
await EnsureTablesAsync(sushi, ("1", 2), ("2", 2), ("Bar 1", 1), ("Tatami 4", 4));

var bistroAdmin = await EnsureUserAsync("pierogi.admin@foodapp.local", "Admin123!", "RestaurantAdmin");
var bistroWaiter = await EnsureUserAsync("pierogi.waiter@foodapp.local", "Waiter123!", "Waiter");
var bistroChef = await EnsureUserAsync("pierogi.chef@foodapp.local", "Chef123!", "Chef");

var sushiAdmin = await EnsureUserAsync("sushi.admin@foodapp.local", "Admin123!", "RestaurantAdmin");
var sushiWaiter = await EnsureUserAsync("sushi.waiter@foodapp.local", "Waiter123!", "Waiter");
var sushiChef = await EnsureUserAsync("sushi.chef@foodapp.local", "Chef123!", "Chef");

await EnsureRestaurantRoleAsync(bistro.Id, bistroAdmin.Id, "RestaurantAdmin");
await EnsureRestaurantRoleAsync(bistro.Id, bistroWaiter.Id, "Waiter");
await EnsureRestaurantRoleAsync(bistro.Id, bistroChef.Id, "Chef");

await EnsureRestaurantRoleAsync(sushi.Id, sushiAdmin.Id, "RestaurantAdmin");
await EnsureRestaurantRoleAsync(sushi.Id, sushiWaiter.Id, "Waiter");
await EnsureRestaurantRoleAsync(sushi.Id, sushiChef.Id, "Chef");

await EnsureRestaurantMenuAsync(
    bistro,
    new SeedCategory(
        1,
        ("pl-PL", "Pierogi", "Klasyczne pierogi lepione na miejscu"),
        ("en-US", "Dumplings", "Classic handmade dumplings"),
        [
            new SeedItem(
                24.90m,
                ("pl-PL", "Pierogi ruskie", "Z twarogiem, ziemniakami i cebulką", "gluten, dairy"),
                ("en-US", "Potato and cheese dumplings", "With cottage cheese, potatoes and onion", "gluten, dairy")),
            new SeedItem(
                28.90m,
                ("pl-PL", "Pierogi z kaczką", "Z wolno pieczoną kaczką i sosem śliwkowym", "gluten"),
                ("en-US", "Duck dumplings", "With slow-roasted duck and plum sauce", "gluten"))
        ]),
    new SeedCategory(
        2,
        ("pl-PL", "Zupy", "Rozgrzewające zupy domowe"),
        ("en-US", "Soups", "Warming house soups"),
        [
            new SeedItem(
                18.00m,
                ("pl-PL", "Żurek", "Na zakwasie z jajkiem i kiełbasą", "gluten, egg"),
                ("en-US", "Sour rye soup", "With egg and sausage", "gluten, egg"))
        ]));

await EnsureRestaurantMenuAsync(
    sushi,
    new SeedCategory(
        1,
        ("pl-PL", "Sushi", "Rolki przygotowywane na zamówienie"),
        ("en-US", "Sushi", "Rolls prepared to order"),
        [
            new SeedItem(
                32.00m,
                ("pl-PL", "California maki", "Krab, awokado, ogórek i sezam", "sesame, shellfish"),
                ("en-US", "California maki", "Crab, avocado, cucumber and sesame", "sesame, shellfish")),
            new SeedItem(
                36.00m,
                ("pl-PL", "Łosoś philadelphia", "Łosoś, serek, awokado", "fish, dairy"),
                ("en-US", "Salmon philadelphia", "Salmon, cream cheese, avocado", "fish, dairy"))
        ]),
    new SeedCategory(
        2,
        ("pl-PL", "Dodatki", "Przekąski i małe talerze"),
        ("en-US", "Sides", "Snacks and small plates"),
        [
            new SeedItem(
                16.00m,
                ("pl-PL", "Edamame", "Z solą morską", "soy"),
                ("en-US", "Edamame", "With sea salt", "soy"))
        ]));

await EnsureIngredientAsync(bistro, "pl-PL", "Mąka pszenna", "en-US", "Wheat flour", 0, 0.006m, "gluten");
await EnsureIngredientAsync(bistro, "pl-PL", "Twaróg", "en-US", "Cottage cheese", 0, 0.018m, "dairy");
await EnsureIngredientAsync(bistro, "pl-PL", "Kaczka", "en-US", "Duck", 0, 0.055m, null);
await EnsureIngredientAsync(sushi, "pl-PL", "Ryż sushi", "en-US", "Sushi rice", 0, 0.012m, null);
await EnsureIngredientAsync(sushi, "pl-PL", "Łosoś", "en-US", "Salmon", 0, 0.070m, "fish");
await EnsureIngredientAsync(sushi, "pl-PL", "Awokado", "en-US", "Avocado", 0, 0.025m, null);

await db.SaveChangesAsync();

Console.WriteLine("Seed complete.");
Console.WriteLine("app.admin@foodapp.local / Admin123!");
Console.WriteLine("pierogi.admin@foodapp.local / Admin123!");
Console.WriteLine("pierogi.waiter@foodapp.local / Waiter123!");
Console.WriteLine("pierogi.chef@foodapp.local / Chef123!");
Console.WriteLine("sushi.admin@foodapp.local / Admin123!");
Console.WriteLine("sushi.waiter@foodapp.local / Waiter123!");
Console.WriteLine("sushi.chef@foodapp.local / Chef123!");
Console.WriteLine("customer@foodapp.local / Customer123!");

async Task<ApplicationUser> EnsureUserAsync(string email, string password, string role)
{
    var user = await users.FindByEmailAsync(email);

    if (user is null)
    {
        user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            WantsOrderStatusEmails = true
        };

        var create = await users.CreateAsync(user, password);
        ThrowIfFailed(create, $"create user {email}");
    }

    if (!await users.IsInRoleAsync(user, role))
    {
        var addRole = await users.AddToRoleAsync(user, role);
        ThrowIfFailed(addRole, $"assign {role} to {email}");
    }

    return user;
}

async Task<Restaurant> EnsureRestaurantAsync(string name, string address)
{
    var restaurant = await db.Restaurants.FirstOrDefaultAsync(r => r.Name == name);
    if (restaurant is null)
    {
        restaurant = new Restaurant
        {
            Name = name,
            Address = address,
            IsActive = true
        };
        db.Restaurants.Add(restaurant);
    }
    else
    {
        restaurant.Address = address;
        restaurant.IsActive = true;
    }

    await db.SaveChangesAsync();
    return restaurant;
}

async Task EnsureTablesAsync(Restaurant restaurant, params (string Label, int Seats)[] tables)
{
    var sort = 1;
    foreach (var table in tables)
    {
        var existing = await db.RestaurantTables
            .FirstOrDefaultAsync(t => t.RestaurantId == restaurant.Id && t.Label == table.Label);

        if (existing is null)
        {
            db.RestaurantTables.Add(new RestaurantTable
            {
                RestaurantId = restaurant.Id,
                Label = table.Label,
                Seats = table.Seats,
                SortOrder = sort,
                IsActive = true,
                IsReservable = true
            });
        }
        else
        {
            existing.Seats = table.Seats;
            existing.SortOrder = sort;
            existing.IsActive = true;
            existing.IsReservable = true;
        }

        sort++;
    }

    await db.SaveChangesAsync();
}

async Task EnsureRestaurantSettingsAsync(
    Restaurant restaurant,
    bool enableDeliveryOrders,
    bool enableReservations,
    decimal deliveryFee)
{
    var settings = await db.RestaurantSettings
        .FirstOrDefaultAsync(x => x.RestaurantId == restaurant.Id);

    if (settings is null)
    {
        settings = new RestaurantSettings
        {
            RestaurantId = restaurant.Id
        };
        db.RestaurantSettings.Add(settings);
    }

    settings.EnableTableOrders = true;
    settings.EnableTakeawayOrders = true;
    settings.EnableDeliveryOrders = enableDeliveryOrders;
    settings.EnablePayInApp = true;
    settings.EnablePayAtCounter = true;
    settings.EnableReservations = enableReservations;
    settings.ReservationRequiresInAppPayment = true;
    settings.ReservationPreorderMinOffsetMinutes = 5;
    settings.ReservationPreorderMaxAfterStartMinutes = 60;
    settings.ReservationStartMinuteOfDay = 17 * 60;
    settings.ReservationLastStartMinuteOfDay = 23 * 60;
    settings.DefaultReservationDurationMinutes = 90;
    settings.ReservationHoldsTableUntilClose = false;
    settings.ReservationGracePeriodMinutes = 15;
    settings.DeliveryFee = deliveryFee;
    settings.EstimatedPreparationBaseMinutes = 10;
    settings.EstimatedPreparationPerItemMinutes = 2;
    settings.SupportedCultures = "pl-PL,en-US";
    settings.DefaultCulture = "pl-PL";

    await db.SaveChangesAsync();
}

async Task EnsureIngredientAsync(
    Restaurant restaurant,
    string plCulture,
    string plName,
    string enCulture,
    string enName,
    int unit,
    decimal costPerUnit,
    string? allergenCode)
{
    var ingredient = await db.Ingredients
        .Include(x => x.Translations)
        .FirstOrDefaultAsync(x =>
            x.RestaurantId == restaurant.Id &&
            x.Translations.Any(t => t.Culture == enCulture && t.Name == enName));

    if (ingredient is null)
    {
        ingredient = new Ingredient
        {
            RestaurantId = restaurant.Id,
            Unit = (Core.Data.Enums.IngredientUnit)unit,
            CostPerUnit = costPerUnit,
            IsActive = true,
            AllergenCode = allergenCode,
            Translations =
            {
                new IngredientTranslation { Culture = plCulture, Name = plName },
                new IngredientTranslation { Culture = enCulture, Name = enName }
            }
        };
        db.Ingredients.Add(ingredient);
    }
    else
    {
        ingredient.Unit = (Core.Data.Enums.IngredientUnit)unit;
        ingredient.CostPerUnit = costPerUnit;
        ingredient.IsActive = true;
        ingredient.AllergenCode = allergenCode;
    }

    await db.SaveChangesAsync();
}

async Task EnsureRestaurantRoleAsync(int restaurantId, string userId, string role)
{
    var exists = await db.RestaurantUserRoles.AnyAsync(x =>
        x.RestaurantId == restaurantId &&
        x.UserId == userId &&
        x.Role == role);

    if (!exists)
    {
        db.RestaurantUserRoles.Add(new RestaurantUserRole
        {
            RestaurantId = restaurantId,
            UserId = userId,
            Role = role
        });
        await db.SaveChangesAsync();
    }
}

async Task EnsureRestaurantMenuAsync(Restaurant restaurant, params SeedCategory[] categories)
{
    foreach (var category in categories)
    {
        var existingCategory = await db.MenuCategories
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c =>
                c.RestaurantId == restaurant.Id &&
                c.Translations.Any(t => t.Culture == "en-US" && t.Name == category.En.Name));

        if (existingCategory is null)
        {
            existingCategory = new MenuCategory
            {
                RestaurantId = restaurant.Id,
                SortOrder = category.SortOrder,
                IsActive = true,
                Translations =
                {
                    new MenuCategoryTranslation
                    {
                        Culture = "pl-PL",
                        Name = category.Pl.Name,
                        Description = category.Pl.Description
                    },
                    new MenuCategoryTranslation
                    {
                        Culture = "en-US",
                        Name = category.En.Name,
                        Description = category.En.Description
                    }
                }
            };
            db.MenuCategories.Add(existingCategory);
            await db.SaveChangesAsync();
        }
        else
        {
            existingCategory.SortOrder = category.SortOrder;
            existingCategory.IsActive = true;
            UpsertCategoryTranslation(existingCategory, "pl-PL", category.Pl);
            UpsertCategoryTranslation(existingCategory, "en-US", category.En);
            await db.SaveChangesAsync();
        }

        foreach (var item in category.Items)
        {
            var existingItem = await db.MenuItems
                .Include(i => i.Translations)
                .FirstOrDefaultAsync(i =>
                    i.MenuCategoryId == existingCategory.Id &&
                    i.Translations.Any(t => t.Culture == "en-US" && t.Name == item.En.Name));

            if (existingItem is null)
            {
                db.MenuItems.Add(new MenuItem
                {
                    MenuCategoryId = existingCategory.Id,
                    CurrentPrice = item.Price,
                    IsAvailable = true,
                    Translations =
                    {
                        new MenuItemTranslation
                        {
                            Culture = "pl-PL",
                            Name = item.Pl.Name,
                            Description = item.Pl.Description,
                            Allergens = item.Pl.Allergens
                        },
                        new MenuItemTranslation
                        {
                            Culture = "en-US",
                            Name = item.En.Name,
                            Description = item.En.Description,
                            Allergens = item.En.Allergens
                        }
                    }
                });
            }
            else
            {
                existingItem.CurrentPrice = item.Price;
                existingItem.IsAvailable = true;
                UpsertItemTranslation(existingItem, "pl-PL", item.Pl);
                UpsertItemTranslation(existingItem, "en-US", item.En);
            }
        }

        await db.SaveChangesAsync();
    }
}

static void UpsertCategoryTranslation(
    MenuCategory category,
    string culture,
    SeedTranslation translation)
{
    var existing = category.Translations.FirstOrDefault(t => t.Culture == culture);
    if (existing is null)
    {
        category.Translations.Add(new MenuCategoryTranslation
        {
            Culture = culture,
            Name = translation.Name,
            Description = translation.Description
        });
        return;
    }

    existing.Name = translation.Name;
    existing.Description = translation.Description;
}

static void UpsertItemTranslation(MenuItem item, string culture, SeedTranslation translation)
{
    var existing = item.Translations.FirstOrDefault(t => t.Culture == culture);
    if (existing is null)
    {
        item.Translations.Add(new MenuItemTranslation
        {
            Culture = culture,
            Name = translation.Name,
            Description = translation.Description,
            Allergens = translation.Allergens
        });
        return;
    }

    existing.Name = translation.Name;
    existing.Description = translation.Description;
    existing.Allergens = translation.Allergens;
}

static void ThrowIfFailed(IdentityResult result, string action)
{
    if (result.Succeeded) return;

    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
    throw new InvalidOperationException($"Failed to {action}: {errors}");
}

record SeedCategory(
    int SortOrder,
    SeedTranslation Pl,
    SeedTranslation En,
    IReadOnlyList<SeedItem> Items);

record SeedItem(decimal Price, SeedTranslation Pl, SeedTranslation En);

record SeedTranslation(string Culture, string Name, string Description, string? Allergens = null)
{
    public static implicit operator SeedTranslation(
        (string Culture, string Name, string Description) value) =>
        new(value.Culture, value.Name, value.Description);

    public static implicit operator SeedTranslation(
        (string Culture, string Name, string Description, string? Allergens) value) =>
        new(value.Culture, value.Name, value.Description, value.Allergens);
}
