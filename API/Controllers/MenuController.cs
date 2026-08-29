using Core.Contracts.Menu;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/menu")]
    public class MenuController : ControllerBase
    {
        private const string DefaultCulture = "pl-PL";

        private readonly IMenuCategoryRepository _categories;
        private readonly IMenuItemRepository _items;
        private readonly IAsyncQueryExecutor _q;
        private readonly IMenuService _menuService;

        public MenuController(
            IMenuCategoryRepository categories,
            IMenuItemRepository items,
            IAsyncQueryExecutor q,
            IMenuService menuService)
        {
            _categories = categories;
            _items = items;
            _q = q;
            _menuService = menuService;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories([FromQuery] int? restaurantId, [FromQuery] string? lang, CancellationToken ct)
        {
            var culture = string.IsNullOrWhiteSpace(lang) ? DefaultCulture : lang;

            var query = _categories.Query()
                .Where(c => c.IsActive)
                .Where(c => restaurantId == null || c.RestaurantId == restaurantId)
                .OrderBy(c => c.SortOrder)
                .Select(c => new
                {
                    c.Id,
                    c.RestaurantId,
                    c.SortOrder,
                    Name =
                        c.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                        ?? c.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Name).FirstOrDefault()
                        ?? c.Translations.Select(t => t.Name).FirstOrDefault()
                });

            var result = await _q.ToListAsync(query, ct);
            return Ok(result);
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetItems([FromQuery] int? restaurantId, [FromQuery] int? categoryId, [FromQuery] string? lang, CancellationToken ct)
        {
            var culture = string.IsNullOrWhiteSpace(lang) ? DefaultCulture : lang;

            var query = _items.Query()
                .Where(m => m.IsAvailable)
                .Where(m => restaurantId == null || (m.Category != null && m.Category.RestaurantId == restaurantId))
                .Where(m => categoryId == null || m.MenuCategoryId == categoryId)
                .OrderBy(m => m.MenuCategoryId)
                .ThenBy(m => m.SortOrder)
                .ThenBy(m => m.Id)
                .Select(m => new MenuItemDto
                {
                    Id = m.Id,
                    MenuCategoryId = m.MenuCategoryId,
                    RestaurantId = m.Category != null ? m.Category.RestaurantId : 0,
                    CurrentPrice = m.CurrentPrice,
                    SortOrder = m.SortOrder,
                    EnableIngredientSwap = m.EnableIngredientSwap,
                    Name =
                        m.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                        ?? m.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Name).FirstOrDefault()
                        ?? m.Translations.Select(t => t.Name).FirstOrDefault(),
                    Description =
                        m.Translations.Where(t => t.Culture == culture).Select(t => t.Description).FirstOrDefault()
                        ?? m.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Description).FirstOrDefault(),
                    Allergens =
                        m.Translations.Where(t => t.Culture == culture).Select(t => t.Allergens).FirstOrDefault()
                        ?? m.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Allergens).FirstOrDefault(),
                    PhotoAssetId = m.PhotoAssetId,
                    PhotoPath = m.PhotoPath
                });

            var result = await _q.ToListAsync(query, ct);
            foreach (var item in result)
            {
                item.PhotoUrl = _menuService.BuildPhotoUrl(item.PhotoPath);
            }

            return Ok(result);
        }

        [HttpGet("items/{id:int}/customization")]
        public async Task<IActionResult> GetCustomization(int id, [FromQuery] string? lang, CancellationToken ct)
        {
            var culture = string.IsNullOrWhiteSpace(lang) ? DefaultCulture : lang;

            var item = await _items.Query()
                .Where(m => m.Id == id && m.IsAvailable)
                .Select(m => new MenuItemCustomizationDto
                {
                    MenuItemId = m.Id,
                    EnableIngredientSwap = m.EnableIngredientSwap,
                    ExtraIngredientPrice = m.Category != null && m.Category.Restaurant != null && m.Category.Restaurant.Settings != null
                        ? m.Category.Restaurant.Settings.ExtraIngredientPrice
                        : 0m,
                    DishIngredients = m.Ingredients
                        .Where(i => i.IsDefault)
                        .OrderBy(i => i.Id)
                        .Select(i => new MenuItemIngredientOptionDto
                        {
                            IngredientId = i.IngredientId,
                            Name = i.Ingredient != null
                                ? (i.Ingredient.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                                   ?? i.Ingredient.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Name).FirstOrDefault()
                                   ?? i.Ingredient.Translations.Select(t => t.Name).FirstOrDefault()
                                   ?? $"Ingredient #{i.IngredientId}")
                                : $"Ingredient #{i.IngredientId}",
                            Quantity = i.Quantity
                        }).ToList(),
                    RemovableIngredientIds = m.Ingredients
                        .Where(i => i.IsDefault && i.IsRemovable)
                        .OrderBy(i => i.Id)
                        .Select(i => i.IngredientId)
                        .ToList(),
                    SubstituteIngredients = m.Ingredients
                        .Where(i => i.IsSubstitute)
                        .OrderBy(i => i.Id)
                        .Select(i => new MenuItemIngredientOptionDto
                        {
                            IngredientId = i.IngredientId,
                            Name = i.Ingredient != null
                                ? (i.Ingredient.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                                   ?? i.Ingredient.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Name).FirstOrDefault()
                                   ?? i.Ingredient.Translations.Select(t => t.Name).FirstOrDefault()
                                   ?? $"Ingredient #{i.IngredientId}")
                                : $"Ingredient #{i.IngredientId}",
                            Quantity = i.Quantity
                        }).ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (item is null)
            {
                return NotFound();
            }

            return Ok(item);
        }
    }
}
