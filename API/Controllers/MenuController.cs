using Microsoft.AspNetCore.Mvc;
using Core.Interfaces;

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

        public MenuController(
            IMenuCategoryRepository categories,
            IMenuItemRepository items,
            IAsyncQueryExecutor q)
        {
            _categories = categories;
            _items = items;
            _q = q;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories([FromQuery] string? lang, CancellationToken ct)
        {
            var culture = string.IsNullOrWhiteSpace(lang) ? DefaultCulture : lang;

            var query = _categories.Query()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new
                {
                    c.Id,
                    Name =
                        c.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                        ?? c.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Name).FirstOrDefault()
                        ?? c.Translations.Select(t => t.Name).FirstOrDefault()
                });

            var result = await _q.ToListAsync(query, ct);
            return Ok(result);
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetItems([FromQuery] int? categoryId, [FromQuery] string? lang, CancellationToken ct)
        {
            var culture = string.IsNullOrWhiteSpace(lang) ? DefaultCulture : lang;

            var query = _items.Query()
                .Where(m => m.IsAvailable)
                .Where(m => categoryId == null || m.MenuCategoryId == categoryId)
                .OrderBy(m => m.Id)
                .Select(m => new
                {
                    m.Id,
                    m.MenuCategoryId,
                    m.CurrentPrice,
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
                    m.PhotoAssetId
                });

            var result = await _q.ToListAsync(query, ct);
            return Ok(result);
        }
    }
}
