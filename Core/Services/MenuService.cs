using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Contracts.Menu;
using Core.Data.Entities;
using Core.Interfaces;

namespace Core.Services
{
    public class MenuService : IMenuService
    {
        private const string DefaultCulture = "pl-PL";

        private readonly IMenuCategoryRepository _categories;
        private readonly IMenuItemRepository _items;
        private readonly IAsyncQueryExecutor _q;
        private readonly IUnitOfWork _uow;

        public MenuService(
            IMenuCategoryRepository categories,
            IMenuItemRepository items,
            IAsyncQueryExecutor q,
            IUnitOfWork uow)
        {
            _categories = categories;
            _items = items;
            _q = q;
            _uow = uow;
        }

        // ---------- Categories ----------
        public async Task<int> CreateCategoryAsync(MenuCategoryCreateDto dto, string userId, CancellationToken ct = default)
        {
            ValidateCategoryTranslations(dto.Translations);

            var entity = new MenuCategory
            {
                RestaurantId = dto.RestaurantId,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                Translations = dto.Translations.Select(t => new MenuCategoryTranslation
                {
                    Culture = t.Culture.Trim(),
                    Name = t.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(t.Description) ? null : t.Description.Trim()
                }).ToList()
            };

            await _categories.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task UpdateCategoryAsync(int id, MenuCategoryUpdateDto dto, string userId, CancellationToken ct = default)
        {
            ValidateCategoryTranslations(dto.Translations);

            var query = _categories.Query(tracked: true).Where(c => c.Id == id);
            var entity = await _q.FirstOrDefaultAsync(query, ct);
            if (entity is null) throw new InvalidOperationException("MenuCategory not found.");

            entity.RestaurantId = dto.RestaurantId;
            entity.IsActive = dto.IsActive;
            entity.SortOrder = dto.SortOrder;

            // Replace-all translations
            entity.Translations.Clear();
            foreach (var t in dto.Translations)
            {
                entity.Translations.Add(new MenuCategoryTranslation
                {
                    Culture = t.Culture.Trim(),
                    Name = t.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(t.Description) ? null : t.Description.Trim()
                });
            }

            await _uow.SaveChangesAsync(ct);
        }

        public async Task DeleteCategoryAsync(int id, string userId, CancellationToken ct = default)
        {
            var entity = await _q.FirstOrDefaultAsync(_categories.Query(tracked: true).Where(c => c.Id == id), ct);
            if (entity is null) return;

            await _categories.RemoveAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<MenuCategoryDto>> GetCategoriesAsync(string? culture, int? restaurantId = null, CancellationToken ct = default)
        {
            var requestedCulture = string.IsNullOrWhiteSpace(culture) ? DefaultCulture : culture.Trim();
            var query = _categories.Query()
                .Where(x => restaurantId == null || x.RestaurantId == restaurantId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .Select(x => new MenuCategoryDto
                {
                    Id = x.Id,
                    RestaurantId = x.RestaurantId,
                    IsActive = x.IsActive,
                    SortOrder = x.SortOrder,
                    Name =
                        x.Translations.Where(t => t.Culture == requestedCulture).Select(t => t.Name).FirstOrDefault()
                        ?? x.Translations.Where(t => t.Culture == x.Restaurant!.Settings!.DefaultCulture).Select(t => t.Name).FirstOrDefault()
                        ?? x.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Name).FirstOrDefault()
                        ?? x.Translations.Select(t => t.Name).FirstOrDefault(),
                    Description =
                        x.Translations.Where(t => t.Culture == requestedCulture).Select(t => t.Description).FirstOrDefault()
                        ?? x.Translations.Where(t => t.Culture == x.Restaurant!.Settings!.DefaultCulture).Select(t => t.Description).FirstOrDefault()
                        ?? x.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Description).FirstOrDefault()
                        ?? x.Translations.Select(t => t.Description).FirstOrDefault(),
                    Translations = x.Translations
                        .OrderBy(t => t.Culture)
                        .Select(t => new MenuCategoryTranslationDto
                        {
                            Culture = t.Culture,
                            Name = t.Name,
                            Description = t.Description
                        })
                        .ToList()
                });

            return await _q.ToListAsync(query, ct);
        }

        public async Task<MenuCategoryDto?> GetCategoryByIdAsync(int id, CancellationToken ct = default)
        {
            var query = _categories.Query()
                .Where(x => x.Id == id)
                .Select(x => new MenuCategoryDto
                {
                    Id = x.Id,
                    RestaurantId = x.RestaurantId,
                    IsActive = x.IsActive,
                    SortOrder = x.SortOrder,

                    Name =
                        x.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Name).FirstOrDefault()
                        ?? x.Translations.Select(t => t.Name).FirstOrDefault(),

                    Description =
                        x.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Description).FirstOrDefault()
                        ?? x.Translations.Select(t => t.Description).FirstOrDefault(),

                    Translations = x.Translations
                        .OrderBy(t => t.Culture)
                        .Select(t => new MenuCategoryTranslationDto
                        {
                            Culture = t.Culture,
                            Name = t.Name,
                            Description = t.Description
                        })
                        .ToList()
                });

            return await _q.FirstOrDefaultAsync(query, ct);
        }

        public async Task UpsertCategoryTranslationAsync(
    int categoryId,
    MenuCategoryTranslationUpsertDto dto,
    string userId,
    CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Culture))
                throw new InvalidOperationException("Culture is required.");
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new InvalidOperationException("Name is required.");

            var culture = dto.Culture.Trim();
            var name = dto.Name.Trim();
            var desc = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            var category = await _q.FirstOrDefaultAsync(
                _categories.Query(tracked: true)
                    .Where(c => c.Id == categoryId),
                ct);

            if (category is null)
                throw new InvalidOperationException("Category not found.");

            var existing = category.Translations.FirstOrDefault(t => t.Culture == culture);

            if (existing is null)
            {
                category.Translations.Add(new MenuCategoryTranslation
                {
                    MenuCategoryId = categoryId,
                    Culture = culture,
                    Name = name,
                    Description = desc
                });
            }
            else
            {
                existing.Name = name;
                existing.Description = desc;
            }

            await _uow.SaveChangesAsync(ct);
        }

        // ---------- Items ----------

        public async Task<IReadOnlyList<MenuItemDto>> GetItemsAsync(string? culture, int? restaurantId = null, CancellationToken ct = default)
        {
            var c = string.IsNullOrWhiteSpace(culture) ? "en-US" : culture.Trim();

            var query =
                _items.Query(tracked: false)
                    .Where(m => restaurantId == null || (m.Category != null && m.Category.RestaurantId == restaurantId))
                    .OrderBy(m => m.MenuCategoryId)
                    .ThenBy(m => m.SortOrder)
                    .ThenBy(m => m.Id)
                    .Select(m => new
                    {
                        Item = new MenuItemDto
                        {
                            Id = m.Id,
                            MenuCategoryId = m.MenuCategoryId,
                            RestaurantId = m.Category != null ? m.Category.RestaurantId : 0,
                            CurrentPrice = m.CurrentPrice,
                            IsAvailable = m.IsAvailable,
                            SortOrder = m.SortOrder,
                            EnableIngredientSwap = m.EnableIngredientSwap,
                            PhotoAssetId = m.PhotoAssetId,
                            PhotoPath = m.PhotoPath,

                            Name = m.Translations
                            .Where(t => t.Culture == c)
                            .Select(t => t.Name)
                            .FirstOrDefault() ?? "(no name)",

                            Description = m.Translations
                            .Where(t => t.Culture == c)
                            .Select(t => t.Description)
                            .FirstOrDefault()
                            ?? m.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Description).FirstOrDefault()
                            ?? m.Translations.Select(t => t.Description).FirstOrDefault(),

                            Allergens = m.Translations
                            .Where(t => t.Culture == c)
                            .Select(t => t.Allergens)
                            .FirstOrDefault()
                            ?? m.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Allergens).FirstOrDefault()
                            ?? m.Translations.Select(t => t.Allergens).FirstOrDefault()
                        },
                        DefaultIngredientAllergenCodes = m.Ingredients
                            .Where(i => i.IsDefault && i.Ingredient != null && i.Ingredient.AllergenCode != null)
                            .Select(i => i.Ingredient!.AllergenCode!)
                            .ToList()
                    });

            var rows = await _q.ToListAsync(query, ct);
            var items = rows.Select(row =>
            {
                var item = row.Item;
                if (string.IsNullOrWhiteSpace(item.Allergens))
                {
                    var derivedAllergens = row.DefaultIngredientAllergenCodes
                        .SelectMany(SplitAllergenCodes)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (derivedAllergens.Count > 0)
                    {
                        item.Allergens = string.Join(", ", derivedAllergens);
                    }
                }

                item.PhotoUrl = BuildPhotoUrl(item.PhotoPath);
                return item;
            }).ToList();

            return items;
        }

        private static IReadOnlyList<string> SplitAllergenCodes(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<MenuItemDto?> GetItemByIdAsync(int id, string? culture, CancellationToken ct = default)
        {
            var requestedCulture = string.IsNullOrWhiteSpace(culture)
                ? DefaultCulture
                : culture.Trim();

            var query = _items.Query()
                .Where(x => x.Id == id)
                .Select(x => new MenuItemDto
                {
                    Id = x.Id,
                    MenuCategoryId = x.MenuCategoryId,
                    RestaurantId = x.Category != null ? x.Category.RestaurantId : 0,
                    CurrentPrice = x.CurrentPrice,
                    IsAvailable = x.IsAvailable,
                    SortOrder = x.SortOrder,
                    EnableIngredientSwap = x.EnableIngredientSwap,
                    PhotoAssetId = x.PhotoAssetId,
                    PhotoPath = x.PhotoPath,

                    Name =
                        x.Translations.Where(t => t.Culture == requestedCulture).Select(t => t.Name).FirstOrDefault()
                        ?? x.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Name).FirstOrDefault()
                        ?? x.Translations.Select(t => t.Name).FirstOrDefault(),

                    Description =
                        x.Translations.Where(t => t.Culture == requestedCulture).Select(t => t.Description).FirstOrDefault()
                        ?? x.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Description).FirstOrDefault()
                        ?? x.Translations.Select(t => t.Description).FirstOrDefault(),

                    Allergens =
                        x.Translations.Where(t => t.Culture == requestedCulture).Select(t => t.Allergens).FirstOrDefault()
                        ?? x.Translations.Where(t => t.Culture == DefaultCulture).Select(t => t.Allergens).FirstOrDefault()
                        ?? x.Translations.Select(t => t.Allergens).FirstOrDefault(),

                    Translations = x.Translations
                        .OrderBy(t => t.Culture)
                        .Select(t => new MenuItemTranslationDto
                        {
                            Culture = t.Culture,
                            Name = t.Name,
                            Description = t.Description,
                            Allergens = t.Allergens
                        })
                        .ToList()
                });

            var item = await _q.FirstOrDefaultAsync(query, ct);
            if (item is not null)
            {
                item.PhotoUrl = BuildPhotoUrl(item.PhotoPath);
            }

            return item;
        }

        public async Task<int> CreateItemAsync(MenuItemCreateDto dto, string userId, CancellationToken ct = default)
        {
            ValidateItem(dto);

            // Optional: check category exists
            var catExists = await _q.AnyAsync(_categories.Query().Where(c => c.Id == dto.MenuCategoryId && c.IsActive), ct);
            if (!catExists) throw new InvalidOperationException("MenuCategory does not exist or is inactive.");

            var entity = new MenuItem
            {
                MenuCategoryId = dto.MenuCategoryId,
                SortOrder = dto.SortOrder,
                CurrentPrice = dto.CurrentPrice,
                IsAvailable = dto.IsAvailable,
                PhotoAssetId = dto.PhotoAssetId,
                PhotoPath = NormalizePhotoPath(dto.PhotoPath),
                Translations = dto.Translations.Select(t => new MenuItemTranslation
                {
                    Culture = t.Culture.Trim(),
                    Name = t.Name.Trim(),
                    Description = t.Description?.Trim(),
                    Allergens = t.Allergens?.Trim()
                }).ToList()
            };

            await _items.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task UpdateItemAsync(int id, MenuItemUpdateDto dto, string userId, CancellationToken ct = default)
        {
            ValidateItemTranslations(dto.Translations);

            var entity = await _q.FirstOrDefaultAsync(_items.Query(tracked: true).Where(m => m.Id == id), ct);
            if (entity is null) throw new InvalidOperationException("MenuItem not found.");

            entity.MenuCategoryId = dto.MenuCategoryId;
            entity.SortOrder = dto.SortOrder;
            entity.IsAvailable = dto.IsAvailable;
            entity.PhotoAssetId = dto.PhotoAssetId;
            entity.PhotoPath = NormalizePhotoPath(dto.PhotoPath);

            // Replace-all translations
            entity.Translations.Clear();
            foreach (var t in dto.Translations)
            {
                entity.Translations.Add(new MenuItemTranslation
                {
                    Culture = t.Culture.Trim(),
                    Name = t.Name.Trim(),
                    Description = t.Description?.Trim(),
                    Allergens = t.Allergens?.Trim()
                });
            }

            await _uow.SaveChangesAsync(ct);
        }

        public async Task DeleteItemAsync(int id, string userId, CancellationToken ct = default)
        {
            var entity = await _q.FirstOrDefaultAsync(_items.Query(tracked: true).Where(m => m.Id == id), ct);
            if (entity is null) return;

            await _items.RemoveAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task ChangePriceAsync(int id, decimal newPrice, string userId, CancellationToken ct = default)
        {
            if (newPrice < 0) throw new ArgumentOutOfRangeException(nameof(newPrice));

            var entity = await _q.FirstOrDefaultAsync(_items.Query(tracked: true).Where(m => m.Id == id), ct);
            if (entity is null) throw new InvalidOperationException("MenuItem not found.");

            entity.CurrentPrice = newPrice;
            await _uow.SaveChangesAsync(ct);
        }

        public async Task SetAvailabilityAsync(int id, bool isAvailable, string userId, CancellationToken ct = default)
        {
            var entity = await _q.FirstOrDefaultAsync(_items.Query(tracked: true).Where(m => m.Id == id), ct);
            if (entity is null) throw new InvalidOperationException("MenuItem not found.");

            entity.IsAvailable = isAvailable;
            await _uow.SaveChangesAsync(ct);
        }

        public async Task UpsertItemTranslationAsync(
            int itemId,
            MenuItemTranslationUpsertDto dto,
            string userId,
            CancellationToken ct = default)
                {
                    if (string.IsNullOrWhiteSpace(dto.Culture))
                        throw new InvalidOperationException("Culture is required.");

                    if (string.IsNullOrWhiteSpace(dto.Name))
                        throw new InvalidOperationException("Name is required.");

                    var culture = dto.Culture.Trim();
                    var name = dto.Name.Trim();
                    var description = string.IsNullOrWhiteSpace(dto.Description)
                        ? null
                        : dto.Description.Trim();

                    var item = await _q.FirstOrDefaultAsync(
                        _items.Query(tracked: true)
                            .Where(m => m.Id == itemId),
                        ct);

                    if (item is null)
                        throw new InvalidOperationException("Menu item not found.");

                    var existing = item.Translations.FirstOrDefault(t => t.Culture == culture);

                    if (existing is null)
                    {
                        item.Translations.Add(new MenuItemTranslation
                        {
                            MenuItemId = itemId,
                            Culture = culture,
                            Name = name,
                            Description = description
                        });
                    }
                    else
                    {
                        existing.Name = name;
                        existing.Description = description;
                    }

                    await _uow.SaveChangesAsync(ct);
                }

        // ---------- Validation helpers ----------
        private static void ValidateCategoryTranslations(List<MenuCategoryTranslationDto> tr)
        {
            if (tr is null || tr.Count == 0) throw new InvalidOperationException("Translations are required.");
            RequireDefaultCulture(tr.Select(x => x.Culture));
            EnsureUniqueCultures(tr.Select(x => x.Culture));
            if (tr.Any(x => string.IsNullOrWhiteSpace(x.Name))) throw new InvalidOperationException("Translation Name is required.");
        }

        private static void ValidateItem(MenuItemCreateDto dto)
        {
            if (dto.CurrentPrice < 0) throw new ArgumentOutOfRangeException(nameof(dto.CurrentPrice));
            ValidateItemTranslations(dto.Translations);
        }

        public string? BuildPhotoUrl(string? photoPath)
        {
            if (string.IsNullOrWhiteSpace(photoPath))
                return null;

            var normalized = photoPath.Replace("\\", "/").Trim();
            return normalized.StartsWith("/") ? normalized : "/" + normalized;
        }

        private static string? NormalizePhotoPath(string? photoPath)
        {
            if (string.IsNullOrWhiteSpace(photoPath))
                return null;

            var normalized = photoPath.Replace("\\", "/").Trim();
            return normalized.StartsWith("/") ? normalized : "/" + normalized;
        }

        private static void ValidateItemTranslations(List<MenuItemTranslationDto> tr)
        {
            if (tr is null || tr.Count == 0) throw new InvalidOperationException("Translations are required.");
            RequireDefaultCulture(tr.Select(x => x.Culture));
            EnsureUniqueCultures(tr.Select(x => x.Culture));
            if (tr.Any(x => string.IsNullOrWhiteSpace(x.Name))) throw new InvalidOperationException("Translation Name is required.");
        }

        private static void RequireDefaultCulture(IEnumerable<string> cultures)
        {
            if (!cultures.Any(c => string.Equals(c?.Trim(), DefaultCulture, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Default culture '{DefaultCulture}' translation is required.");
        }

        private static void EnsureUniqueCultures(IEnumerable<string> cultures)
        {
            var dup = cultures
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim().ToLowerInvariant())
                .GroupBy(c => c)
                .FirstOrDefault(g => g.Count() > 1);

            if (dup != null) throw new InvalidOperationException($"Duplicate culture '{dup.Key}' in translations.");
        }
    }
}
