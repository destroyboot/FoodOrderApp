using API.Localization;
using Core.Contracts.Platform;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/platform")]
    public class PlatformController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAppLocalizationFileStore _localizationStore;

        public PlatformController(AppDbContext db, IAppLocalizationFileStore localizationStore)
        {
            _db = db;
            _localizationStore = localizationStore;
        }

        [HttpGet("languages")]
        [AllowAnonymous]
        public async Task<ActionResult<AppLanguagePayloadDto>> GetLanguages(CancellationToken ct)
        {
            var languages = await _db.AppLanguages
                .AsNoTracking()
                .Where(x => x.IsActive)
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

            var defaultCulture = languages.FirstOrDefault(x => x.IsDefault)?.Culture
                ?? languages.FirstOrDefault()?.Culture
                ?? "pl-PL";

            return Ok(new AppLanguagePayloadDto
            {
                DefaultCulture = defaultCulture,
                Languages = languages
            });
        }

        [HttpGet("texts")]
        [AllowAnonymous]
        public async Task<ActionResult<AppTextDictionaryDto>> GetTexts([FromQuery] string? culture, CancellationToken ct)
        {
            var languages = await _db.AppLanguages
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            var defaultCulture = languages.FirstOrDefault(x => x.IsDefault)?.Culture
                ?? languages.FirstOrDefault()?.Culture
                ?? "pl-PL";
            var requestedCulture = string.IsNullOrWhiteSpace(culture) ? defaultCulture : culture.Trim();

            var dictionary = await _localizationStore.GetDictionaryAsync(requestedCulture, defaultCulture, ct);
            return Ok(dictionary);
        }
    }
}
