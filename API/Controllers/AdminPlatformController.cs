using API.Localization;
using Core.Contracts.Platform;
using Core.Data.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/platform")]
    public class AdminPlatformController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAppLocalizationFileStore _localizationStore;

        public AdminPlatformController(AppDbContext db, IAppLocalizationFileStore localizationStore)
        {
            _db = db;
            _localizationStore = localizationStore;
        }

        [HttpGet("languages")]
        public async Task<ActionResult<IReadOnlyList<AppLanguageDto>>> GetLanguages(CancellationToken ct)
        {
            var items = await _db.AppLanguages
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.DisplayName)
                .Select(x => new AppLanguageDto
                {
                    Id = x.Id,
                    Culture = x.Culture,
                    DisplayName = x.DisplayName,
                    NativeName = x.NativeName,
                    IsActive = x.IsActive,
                    IsDefault = x.IsDefault,
                    SortOrder = x.SortOrder
                })
                .ToListAsync(ct);

            return Ok(items);
        }

        [HttpPut("languages")]
        public async Task<IActionResult> SaveLanguages(AppLanguageBulkUpsertDto dto, CancellationToken ct)
        {
            if (dto.Items.Count == 0)
                throw new InvalidOperationException("At least one app language is required.");

            var normalizedCultures = dto.Items
                .Select(x => (x.Culture ?? "").Trim())
                .ToList();

            if (normalizedCultures.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("Every language must have a culture code.");

            if (normalizedCultures.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalizedCultures.Count)
                throw new InvalidOperationException("Duplicate language cultures are not allowed.");

            var defaultCount = dto.Items.Count(x => x.IsDefault);
            if (defaultCount != 1)
                throw new InvalidOperationException("Exactly one default language is required.");

            var existing = await _db.AppLanguages.ToListAsync(ct);
            _db.AppLanguages.RemoveRange(existing.Where(x => dto.Items.All(i => i.Id != x.Id)));

            foreach (var item in dto.Items)
            {
                var culture = item.Culture.Trim();
                var entity = existing.FirstOrDefault(x => x.Id == item.Id && item.Id > 0);
                if (entity is null)
                {
                    entity = new AppLanguage();
                    _db.AppLanguages.Add(entity);
                }

                entity.Culture = culture;
                entity.DisplayName = item.DisplayName.Trim();
                entity.NativeName = item.NativeName.Trim();
                entity.IsActive = item.IsActive;
                entity.IsDefault = item.IsDefault;
                entity.SortOrder = item.SortOrder;
            }

            await _db.SaveChangesAsync(ct);
            await _localizationStore.EnsureCultureFilesAsync(normalizedCultures, dto.Items.First(x => x.IsDefault).Culture.Trim(), ct);
            return NoContent();
        }

        [HttpGet("texts")]
        public async Task<ActionResult<IReadOnlyList<AppTextTranslationDto>>> GetTexts(CancellationToken ct)
        {
            var items = await _localizationStore.GetAllAsync(ct);
            return Ok(items);
        }

        [HttpPut("texts")]
        public async Task<IActionResult> SaveTexts(AppTextBulkUpsertDto dto, CancellationToken ct)
        {
            foreach (var item in dto.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Key))
                    throw new InvalidOperationException("Translation key is required.");
                if (string.IsNullOrWhiteSpace(item.Culture))
                    throw new InvalidOperationException("Translation culture is required.");
                if (string.IsNullOrWhiteSpace(item.Value))
                    throw new InvalidOperationException("Translation value is required.");
            }

            var languages = await _db.AppLanguages
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);
            var defaultCulture = languages.FirstOrDefault(x => x.IsDefault)?.Culture
                ?? languages.FirstOrDefault()?.Culture
                ?? "pl-PL";

            await _localizationStore.SaveAllAsync(dto.Items, languages.Select(x => x.Culture).ToList(), defaultCulture, ct);
            return NoContent();
        }
    }
}
