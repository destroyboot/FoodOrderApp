using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Data.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seeding
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            // Avoid reseeding
            if (await db.MenuCategories.AnyAsync())
                return;

            // --------------------
            // Categories
            // --------------------
            var starters = new MenuCategory
            {
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
                SortOrder = 2,
                IsActive = true,
                Translations =
            {
                new MenuCategoryTranslation { Culture = "pl-PL", Name = "Dania główne" },
                new MenuCategoryTranslation { Culture = "en-US", Name = "Main dishes" }
            }
            };

            db.MenuCategories.AddRange(starters, mains);
            await db.SaveChangesAsync();

            // --------------------
            // Menu items
            // --------------------
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
                    Name = "Stek wołowy",
                    Description = "Stek wołowy z grilla",
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
    }
}
