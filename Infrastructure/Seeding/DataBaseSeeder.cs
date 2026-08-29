using Core.Data.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seeding
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            await EnsureAppLanguagesAsync(db);
            await EnsureAllergensAsync(db);
            await db.SaveChangesAsync();

            if (await db.Restaurants.AnyAsync())
                return;

            var demoRestaurant = new Restaurant
            {
                Name = "Demo Restaurant",
                Address = "Demo Street 1",
                IsActive = true,
                Settings = new RestaurantSettings(),
                Tables =
                {
                    new RestaurantTable { Label = "1", Seats = 2, SortOrder = 1, IsActive = true, IsReservable = true },
                    new RestaurantTable { Label = "2", Seats = 4, SortOrder = 2, IsActive = true, IsReservable = true },
                    new RestaurantTable { Label = "Patio 1", Seats = 4, SortOrder = 3, IsActive = true, IsReservable = true }
                }
            };

            db.Restaurants.Add(demoRestaurant);
            await db.SaveChangesAsync();

            var starters = new MenuCategory
            {
                RestaurantId = demoRestaurant.Id,
                SortOrder = 1,
                IsActive = true,
                Translations =
                {
                    new MenuCategoryTranslation { Culture = "pl-PL", Name = "Przystawki" },
                    new MenuCategoryTranslation { Culture = "en-US", Name = "Starters" }
                }
            };

            var mains = new MenuCategory
            {
                RestaurantId = demoRestaurant.Id,
                SortOrder = 2,
                IsActive = true,
                Translations =
                {
                    new MenuCategoryTranslation { Culture = "pl-PL", Name = "Dania glowne" },
                    new MenuCategoryTranslation { Culture = "en-US", Name = "Main dishes" }
                }
            };

            db.MenuCategories.AddRange(starters, mains);
            await db.SaveChangesAsync();

            var soup = new MenuItem
            {
                MenuCategoryId = starters.Id,
                CurrentPrice = 12.50m,
                IsAvailable = true,
                Translations =
                {
                    new MenuItemTranslation
                    {
                        Culture = "pl-PL",
                        Name = "Zupa pomidorowa",
                        Description = "Zupa pomidorowa z makaronem",
                        Allergens = "gluten"
                    },
                    new MenuItemTranslation
                    {
                        Culture = "en-US",
                        Name = "Tomato soup",
                        Description = "Classic tomato soup with pasta",
                        Allergens = "gluten"
                    }
                }
            };

            var steak = new MenuItem
            {
                MenuCategoryId = mains.Id,
                CurrentPrice = 39.90m,
                IsAvailable = true,
                Translations =
                {
                    new MenuItemTranslation
                    {
                        Culture = "pl-PL",
                        Name = "Stek wolowy",
                        Description = "Stek wolowy z grilla",
                        Allergens = null
                    },
                    new MenuItemTranslation
                    {
                        Culture = "en-US",
                        Name = "Beef steak",
                        Description = "Grilled beef steak",
                        Allergens = null
                    }
                }
            };

            db.MenuItems.AddRange(soup, steak);
            await db.SaveChangesAsync();
        }

        private static async Task EnsureAppLanguagesAsync(AppDbContext db)
        {
            var existing = await db.AppLanguages.ToListAsync();

            EnsureLanguage(existing, db, "pl-PL", "Polish", "Polski", isDefault: true, sortOrder: 1);
            EnsureLanguage(existing, db, "en-US", "English", "English", isDefault: false, sortOrder: 2);
        }

        private static async Task EnsureAllergensAsync(AppDbContext db)
        {
            var existing = await db.Allergens.ToListAsync();
            var defaults = new (string Code, string Name, int SortOrder)[]
            {
                ("gluten", "Gluten", 1),
                ("crustaceans", "Crustaceans", 2),
                ("eggs", "Eggs", 3),
                ("fish", "Fish", 4),
                ("peanuts", "Peanuts", 5),
                ("soy", "Soy", 6),
                ("milk", "Milk", 7),
                ("nuts", "Nuts", 8),
                ("celery", "Celery", 9),
                ("mustard", "Mustard", 10),
                ("sesame", "Sesame", 11),
                ("sulphites", "Sulphites", 12),
                ("lupin", "Lupin", 13),
                ("molluscs", "Molluscs", 14),
            };

            foreach (var item in defaults)
            {
                var entity = existing.FirstOrDefault(x => x.Code == item.Code);
                if (entity is null)
                {
                    db.Allergens.Add(new Allergen
                    {
                        Code = item.Code,
                        Name = item.Name,
                        IsActive = true,
                        SortOrder = item.SortOrder
                    });
                }
                else
                {
                    entity.Name = item.Name;
                    entity.IsActive = true;
                    entity.SortOrder = item.SortOrder;
                }
            }
        }

        private static void EnsureLanguage(
            IReadOnlyCollection<AppLanguage> existing,
            AppDbContext db,
            string culture,
            string displayName,
            string nativeName,
            bool isDefault,
            int sortOrder)
        {
            var entity = existing.FirstOrDefault(x => x.Culture == culture);
            if (entity is null)
            {
                db.AppLanguages.Add(new AppLanguage
                {
                    Culture = culture,
                    DisplayName = displayName,
                    NativeName = nativeName,
                    IsActive = true,
                    IsDefault = isDefault,
                    SortOrder = sortOrder
                });
                return;
            }

            entity.DisplayName = displayName;
            entity.NativeName = nativeName;
            entity.IsActive = true;
            entity.SortOrder = sortOrder;
            if (isDefault)
            {
                entity.IsDefault = true;
            }
        }
    }
}
