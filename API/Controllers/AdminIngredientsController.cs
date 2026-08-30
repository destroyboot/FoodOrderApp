using Core.Contracts.Ingredients;
using Core.Contracts.Menu;
using Core.Data.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/ingredients")]
    [Authorize(Roles = "Admin,RestaurantAdmin")]
    public class AdminIngredientsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminIngredientsController(AppDbContext db) => _db = db;

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing user id.");

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<IngredientDto>>> Get([FromQuery] int? restaurantId, [FromQuery] string? culture, CancellationToken ct)
        {
            var restaurantIds = await GetReadableRestaurantIdsAsync(restaurantId, ct);

            var items = await _db.Ingredients
                .AsNoTracking()
                .Include(x => x.Translations)
                .Where(x => restaurantIds == null || restaurantIds.Contains(x.RestaurantId))
                .OrderBy(x => x.Id)
                .ToListAsync(ct);

            return Ok(items.Select(x => MapIngredient(x, culture)).ToList());
        }

        [HttpGet("allergens")]
        public async Task<ActionResult<IReadOnlyList<AllergenDto>>> GetAllergens([FromQuery] string? culture, CancellationToken ct)
        {
            var items = await _db.Allergens
                .AsNoTracking()
                .Include(x => x.Translations)
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(ct);

            return Ok(items.Select(x => new AllergenDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = ResolveAllergenName(x, culture),
                IsActive = x.IsActive,
                SortOrder = x.SortOrder
            }).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> Create(IngredientUpsertDto dto, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(dto.RestaurantId, ct);
            Validate(dto);

            var ingredient = new Ingredient
            {
                RestaurantId = dto.RestaurantId,
                Unit = dto.Unit,
                CostPerUnit = dto.CostPerUnit,
                IsActive = dto.IsActive,
                AllergenCode = JoinAllergenCodes(dto.AllergenCodes, dto.AllergenCode),
                Translations = dto.Translations.Select(t => new IngredientTranslation
                {
                    Culture = t.Culture.Trim(),
                    Name = t.Name.Trim()
                }).ToList()
            };

            _db.Ingredients.Add(ingredient);
            await _db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(Get), new { restaurantId = dto.RestaurantId }, new { ingredient.Id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, IngredientUpsertDto dto, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(dto.RestaurantId, ct);
            Validate(dto);

            var ingredient = await _db.Ingredients
                .Include(x => x.Translations)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (ingredient is null) return NotFound();

            await EnsureCanManageRestaurantAsync(ingredient.RestaurantId, ct);
            ingredient.RestaurantId = dto.RestaurantId;
            ingredient.Unit = dto.Unit;
            ingredient.CostPerUnit = dto.CostPerUnit;
            ingredient.IsActive = dto.IsActive;
            ingredient.AllergenCode = JoinAllergenCodes(dto.AllergenCodes, dto.AllergenCode);
            ingredient.Translations.Clear();
            foreach (var translation in dto.Translations)
            {
                ingredient.Translations.Add(new IngredientTranslation
                {
                    Culture = translation.Culture.Trim(),
                    Name = translation.Name.Trim()
                });
            }

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpGet("menu-items/{menuItemId:int}")]
        public async Task<ActionResult<IReadOnlyList<MenuItemIngredientDto>>> GetRecipe(int menuItemId, [FromQuery] string? culture, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(await GetMenuItemRestaurantIdAsync(menuItemId, ct), ct);

            var result = await _db.MenuItemIngredients
                .AsNoTracking()
                .Include(x => x.Ingredient!)
                    .ThenInclude(x => x.Translations)
                .Where(x => x.MenuItemId == menuItemId)
                .Select(x => new MenuItemIngredientDto
                {
                    Id = x.Id,
                    MenuItemId = x.MenuItemId,
                    IngredientId = x.IngredientId,
                    IngredientName = x.Ingredient!.Translations
                        .Where(t => culture == null || t.Culture == culture)
                        .Select(t => t.Name)
                        .FirstOrDefault(),
                    Quantity = x.Quantity
                })
                .ToListAsync(ct);

            return Ok(result);
        }

        [HttpGet("menu-items/{menuItemId:int}/customization")]
        public async Task<ActionResult<MenuItemCustomizationDto>> GetCustomization(int menuItemId, [FromQuery] string? culture, CancellationToken ct)
        {
            var restaurantId = await GetMenuItemRestaurantIdAsync(menuItemId, ct);
            await EnsureCanManageRestaurantAsync(restaurantId, ct);

            var item = await _db.MenuItems
                .AsNoTracking()
                .Include(x => x.Ingredients)
                    .ThenInclude(x => x.Ingredient!)
                        .ThenInclude(x => x.Translations)
                .Include(x => x.Category!)
                    .ThenInclude(x => x.Restaurant!)
                        .ThenInclude(x => x.Settings)
                .FirstOrDefaultAsync(x => x.Id == menuItemId, ct);

            if (item is null)
                return NotFound();

            var resolvedCulture = string.IsNullOrWhiteSpace(culture) ? "pl-PL" : culture.Trim();
            string ResolveIngredientName(Ingredient ingredient) =>
                ingredient.Translations.Where(t => t.Culture == resolvedCulture).Select(t => t.Name).FirstOrDefault()
                ?? ingredient.Translations.Where(t => t.Culture == "pl-PL").Select(t => t.Name).FirstOrDefault()
                ?? ingredient.Translations.Select(t => t.Name).FirstOrDefault()
                ?? $"Ingredient #{ingredient.Id}";

            return Ok(new MenuItemCustomizationDto
            {
                MenuItemId = item.Id,
                EnableIngredientSwap = item.EnableIngredientSwap,
                ExtraIngredientPrice = item.Category?.Restaurant?.Settings?.ExtraIngredientPrice ?? 0m,
                DishIngredients = item.Ingredients
                    .Where(x => x.IsDefault)
                    .OrderBy(x => x.Id)
                    .Select(x => new MenuItemIngredientOptionDto
                    {
                        IngredientId = x.IngredientId,
                        Name = x.Ingredient == null ? $"Ingredient #{x.IngredientId}" : ResolveIngredientName(x.Ingredient),
                        Quantity = x.Quantity
                    })
                    .ToList(),
                RemovableIngredientIds = item.Ingredients
                    .Where(x => x.IsDefault && x.IsRemovable)
                    .OrderBy(x => x.Id)
                    .Select(x => x.IngredientId)
                    .ToList(),
                SubstituteIngredients = item.Ingredients
                    .Where(x => x.IsSubstitute)
                    .OrderBy(x => x.Id)
                    .Select(x => new MenuItemIngredientOptionDto
                    {
                        IngredientId = x.IngredientId,
                        Name = x.Ingredient == null ? $"Ingredient #{x.IngredientId}" : ResolveIngredientName(x.Ingredient),
                        Quantity = x.Quantity
                    })
                    .ToList()
            });
        }

        [HttpPut("menu-items/{menuItemId:int}/customization")]
        public async Task<IActionResult> SaveCustomization(int menuItemId, MenuItemCustomizationUpsertDto dto, CancellationToken ct)
        {
            var restaurantId = await GetMenuItemRestaurantIdAsync(menuItemId, ct);
            await EnsureCanManageRestaurantAsync(restaurantId, ct);

            var item = await _db.MenuItems
                .Include(x => x.Ingredients)
                .FirstOrDefaultAsync(x => x.Id == menuItemId, ct);
            if (item is null) return NotFound();

            var dishIds = dto.DishIngredientIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();
            var removableIds = dto.RemovableIngredientIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();
            var substituteIds = dto.SubstituteIngredientIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            var overlapping = dishIds.Intersect(substituteIds).ToList();
            if (overlapping.Count > 0)
                throw new InvalidOperationException("An ingredient cannot be both default and substitute for the same item.");

            if (removableIds.Any(id => !dishIds.Contains(id)))
                throw new InvalidOperationException("Only dish ingredients can be marked as swappable.");

            var requestedIngredientIds = dishIds.Concat(substituteIds).Distinct().ToList();
            if (requestedIngredientIds.Count > 0)
            {
                var validIds = await _db.Ingredients
                    .Where(x => x.RestaurantId == restaurantId && requestedIngredientIds.Contains(x.Id) && x.IsActive)
                    .Select(x => x.Id)
                    .ToListAsync(ct);

                if (validIds.Count != requestedIngredientIds.Count)
                    throw new InvalidOperationException("All selected ingredients must belong to the same restaurant and be active.");
            }

            item.EnableIngredientSwap = dto.EnableIngredientSwap;
            _db.MenuItemIngredients.RemoveRange(item.Ingredients);

            foreach (var ingredientId in dishIds)
            {
                item.Ingredients.Add(new MenuItemIngredient
                {
                    MenuItemId = menuItemId,
                    IngredientId = ingredientId,
                    Quantity = 1m,
                    IsDefault = true,
                    IsRemovable = removableIds.Contains(ingredientId),
                    IsSubstitute = false
                });
            }

            foreach (var ingredientId in substituteIds)
            {
                item.Ingredients.Add(new MenuItemIngredient
                {
                    MenuItemId = menuItemId,
                    IngredientId = ingredientId,
                    Quantity = 1m,
                    IsDefault = false,
                    IsRemovable = false,
                    IsSubstitute = true
                });
            }

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpPost("menu-items/{menuItemId:int}")]
        public async Task<IActionResult> AddRecipeIngredient(int menuItemId, MenuItemIngredientUpsertDto dto, CancellationToken ct)
        {
            var restaurantId = await GetMenuItemRestaurantIdAsync(menuItemId, ct);
            await EnsureCanManageRestaurantAsync(restaurantId, ct);

            var ingredientRestaurantId = await _db.Ingredients
                .Where(x => x.Id == dto.IngredientId)
                .Select(x => (int?)x.RestaurantId)
                .FirstOrDefaultAsync(ct);

            if (ingredientRestaurantId != restaurantId)
                throw new InvalidOperationException("Ingredient must belong to the same restaurant as the menu item.");

            if (dto.Quantity <= 0)
                throw new InvalidOperationException("Quantity must be greater than zero.");

            _db.MenuItemIngredients.Add(new MenuItemIngredient
            {
                MenuItemId = menuItemId,
                IngredientId = dto.IngredientId,
                Quantity = dto.Quantity,
                IsDefault = true,
                IsRemovable = false,
                IsSubstitute = false
            });
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpDelete("menu-item-ingredients/{id:int}")]
        public async Task<IActionResult> DeleteRecipeIngredient(int id, CancellationToken ct)
        {
            var row = await _db.MenuItemIngredients
                .Include(x => x.MenuItem!)
                    .ThenInclude(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (row is null) return NotFound();

            await EnsureCanManageRestaurantAsync(row.MenuItem!.Category!.RestaurantId, ct);
            _db.MenuItemIngredients.Remove(row);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        private async Task EnsureCanManageRestaurantAsync(int restaurantId, CancellationToken ct)
        {
            if (User.IsInRole("Admin")) return;

            var allowed = await _db.RestaurantUserRoles.AnyAsync(x =>
                x.RestaurantId == restaurantId &&
                x.UserId == UserId &&
                x.Role == "RestaurantAdmin", ct);

            if (!allowed)
                throw new InvalidOperationException("You are not allowed to manage this restaurant.");
        }

        private async Task<List<int>?> GetReadableRestaurantIdsAsync(int? restaurantId, CancellationToken ct)
        {
            if (User.IsInRole("Admin"))
                return restaurantId.HasValue ? new List<int> { restaurantId.Value } : null;

            var allowedIds = await _db.RestaurantUserRoles
                .Where(x => x.UserId == UserId && x.Role == "RestaurantAdmin")
                .Select(x => x.RestaurantId)
                .Distinct()
                .ToListAsync(ct);

            if (allowedIds.Count == 0)
                throw new InvalidOperationException("You are not assigned to a restaurant.");

            if (restaurantId.HasValue)
            {
                if (!allowedIds.Contains(restaurantId.Value))
                    throw new InvalidOperationException("You are not allowed to manage this restaurant.");

                return new List<int> { restaurantId.Value };
            }

            return allowedIds;
        }

        private async Task<int> GetMenuItemRestaurantIdAsync(int menuItemId, CancellationToken ct)
        {
            var restaurantId = await _db.MenuItems
                .Where(x => x.Id == menuItemId)
                .Select(x => (int?)x.Category!.RestaurantId)
                .FirstOrDefaultAsync(ct);

            return restaurantId ?? throw new KeyNotFoundException("Menu item not found.");
        }

        private static IngredientDto MapIngredient(Ingredient item, string? culture)
        {
            var name = item.Translations
                .Where(t => culture == null || t.Culture == culture)
                .Select(t => t.Name)
                .FirstOrDefault()
                ?? item.Translations.Select(t => t.Name).FirstOrDefault();

            return new IngredientDto
            {
                Id = item.Id,
                RestaurantId = item.RestaurantId,
                Unit = item.Unit,
                CostPerUnit = item.CostPerUnit,
                IsActive = item.IsActive,
                AllergenCode = item.AllergenCode,
                AllergenCodes = SplitAllergenCodes(item.AllergenCode),
                Name = name,
                Translations = item.Translations.Select(t => new IngredientTranslationDto
                {
                    Culture = t.Culture,
                    Name = t.Name
                }).ToList()
            };
        }

        private static string ResolveAllergenName(Allergen item, string? culture)
        {
            var resolvedCulture = string.IsNullOrWhiteSpace(culture) ? "pl-PL" : culture.Trim();
            return item.Translations
                .Where(t => t.Culture == resolvedCulture)
                .Select(t => t.Name)
                .FirstOrDefault()
                ?? item.Translations
                    .Where(t => t.Culture == "pl-PL")
                    .Select(t => t.Name)
                    .FirstOrDefault()
                ?? item.Translations.Select(t => t.Name).FirstOrDefault()
                ?? item.Name;
        }

        private static void Validate(IngredientUpsertDto dto)
        {
            if (dto.RestaurantId <= 0)
                throw new InvalidOperationException("Restaurant is required.");

            if (dto.CostPerUnit < 0)
                throw new InvalidOperationException("Cost cannot be negative.");

            if (dto.Translations.Count == 0 || dto.Translations.Any(t => string.IsNullOrWhiteSpace(t.Culture) || string.IsNullOrWhiteSpace(t.Name)))
                throw new InvalidOperationException("At least one valid translation is required.");
        }

        private static List<string> SplitAllergenCodes(string? value)
            => string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

        private static string? JoinAllergenCodes(IEnumerable<string>? values, string? fallbackSingleValue)
        {
            var normalized = (values ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalized.Count > 0)
                return string.Join(",", normalized);

            return TrimOrNull(fallbackSingleValue);
        }

        private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
