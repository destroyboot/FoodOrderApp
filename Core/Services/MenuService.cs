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
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                Translations = dto.Translations.Select(t => new MenuCategoryTranslation
                {
                    Culture = t.Culture.Trim(),
                    Name = t.Name.Trim()
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

            entity.IsActive = dto.IsActive;
            entity.SortOrder = dto.SortOrder;

            // Replace-all translations
            entity.Translations.Clear();
            foreach (var t in dto.Translations)
            {
                entity.Translations.Add(new MenuCategoryTranslation
                {
                    Culture = t.Culture.Trim(),
                    Name = t.Name.Trim()
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

        // ---------- Items ----------
        public async Task<int> CreateItemAsync(MenuItemCreateDto dto, string userId, CancellationToken ct = default)
        {
            ValidateItem(dto);

            // Optional: check category exists
            var catExists = await _q.AnyAsync(_categories.Query().Where(c => c.Id == dto.MenuCategoryId && c.IsActive), ct);
            if (!catExists) throw new InvalidOperationException("MenuCategory does not exist or is inactive.");

            var entity = new MenuItem
            {
                MenuCategoryId = dto.MenuCategoryId,
                CurrentPrice = dto.CurrentPrice,
                IsAvailable = dto.IsAvailable,
                PhotoAssetId = dto.PhotoAssetId,
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
            entity.IsAvailable = dto.IsAvailable;
            entity.PhotoAssetId = dto.PhotoAssetId;

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
