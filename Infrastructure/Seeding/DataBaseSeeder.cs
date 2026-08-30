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
            await EnsureCuisinesAsync(db);
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
            var existing = await db.Allergens
                .Include(x => x.Translations)
                .ToListAsync();
            var defaults = new (string Code, string Name, string PlName, string EnName, int SortOrder)[]
            {
                ("gluten", "Gluten", "Gluten", "Gluten", 1),
                ("crustaceans", "Crustaceans", "Skorupiaki", "Crustaceans", 2),
                ("eggs", "Eggs", "Jaja", "Eggs", 3),
                ("fish", "Fish", "Ryby", "Fish", 4),
                ("peanuts", "Peanuts", "Orzeszki ziemne", "Peanuts", 5),
                ("soy", "Soy", "Soja", "Soy", 6),
                ("milk", "Milk", "Mleko", "Milk", 7),
                ("nuts", "Nuts", "Orzechy", "Nuts", 8),
                ("celery", "Celery", "Seler", "Celery", 9),
                ("mustard", "Mustard", "Gorczyca", "Mustard", 10),
                ("sesame", "Sesame", "Sezam", "Sesame", 11),
                ("sulphites", "Sulphites", "Siarczyny", "Sulphites", 12),
                ("lupin", "Lupin", "Łubin", "Lupin", 13),
                ("molluscs", "Molluscs", "Mięczaki", "Molluscs", 14),
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
                    entity = db.Allergens.Local.First(x => x.Code == item.Code);
                }
                else
                {
                    entity.Name = item.Name;
                    entity.IsActive = true;
                    entity.SortOrder = item.SortOrder;
                }

                EnsureAllergenTranslation(entity, "pl-PL", item.PlName);
                EnsureAllergenTranslation(entity, "en-US", item.EnName);
            }
        }

        private static async Task EnsureCuisinesAsync(AppDbContext db)
        {
            var existing = await db.Cuisines
                .Include(x => x.Translations)
                .ToListAsync();
            var defaults = new (string Code, string PlName, string EnName, int SortOrder)[]
            {
                ("Polish", "Polska", "Polish", 1),
                ("Italian", "Włoska", "Italian", 2),
                ("Japanese", "Japońska", "Japanese", 3),
                ("Sushi", "Sushi", "Sushi", 4),
                ("Pierogi", "Pierogi", "Pierogi", 5),
                ("American", "Amerykańska", "American", 6),
                ("Mexican", "Meksykańska", "Mexican", 7),
                ("Indian", "Indyjska", "Indian", 8),
                ("Thai", "Tajska", "Thai", 9),
                ("Vegetarian", "Wegetariańska", "Vegetarian", 10),
                ("Vegan", "Wegańska", "Vegan", 11),
                ("Cafe", "Kawiarnia", "Cafe", 12),
            };

            foreach (var item in defaults)
            {
                var entity = existing.FirstOrDefault(x => x.Name == item.Code);
                if (entity is null)
                {
                    db.Cuisines.Add(new Cuisine
                    {
                        Name = item.Code,
                        IsActive = true,
                        SortOrder = item.SortOrder
                    });
                    entity = db.Cuisines.Local.First(x => x.Name == item.Code);
                }
                else
                {
                    entity.IsActive = true;
                    entity.SortOrder = item.SortOrder;
                }

                EnsureCuisineTranslation(entity, "pl-PL", item.PlName);
                EnsureCuisineTranslation(entity, "en-US", item.EnName);
            }
        }

        private static void EnsureCuisineTranslation(Cuisine cuisine, string culture, string name)
        {
            var translation = cuisine.Translations.FirstOrDefault(x => x.Culture == culture);
            if (translation is null)
            {
                cuisine.Translations.Add(new CuisineTranslation
                {
                    Culture = culture,
                    Name = name
                });
                return;
            }

            translation.Name = name;
        }

        private static void EnsureAllergenTranslation(Allergen allergen, string culture, string name)
        {
            var translation = allergen.Translations.FirstOrDefault(x => x.Culture == culture);
            if (translation is null)
            {
                allergen.Translations.Add(new AllergenTranslation
                {
                    Culture = culture,
                    Name = name
                });
                return;
            }

            translation.Name = name;
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
