using System.Text.Json;
using Core.Contracts.Platform;

namespace API.Localization;

public interface IAppLocalizationFileStore
{
    Task<IReadOnlyList<AppTextTranslationDto>> GetAllAsync(CancellationToken ct);
    Task<AppTextDictionaryDto> GetDictionaryAsync(string requestedCulture, string defaultCulture, CancellationToken ct);
    Task SaveAllAsync(IEnumerable<AppTextTranslationDto> items, IReadOnlyCollection<string> activeCultures, string defaultCulture, CancellationToken ct);
    Task EnsureCultureFilesAsync(IReadOnlyCollection<string> activeCultures, string defaultCulture, CancellationToken ct);
}

public sealed class AppLocalizationFileStore : IAppLocalizationFileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IWebHostEnvironment _environment;

    public AppLocalizationFileStore(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<IReadOnlyList<AppTextTranslationDto>> GetAllAsync(CancellationToken ct)
    {
        EnsureRootExists();

        var files = Directory.GetFiles(GetRootPath(), "*.json")
            .OrderBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var items = new List<AppTextTranslationDto>();
        var nextId = 1;
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var culture = Path.GetFileNameWithoutExtension(file);
            var resource = await ReadResourceFileAsync(culture, ct);
            foreach (var entry in resource.Entries.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                items.Add(new AppTextTranslationDto
                {
                    Id = nextId++,
                    Key = entry.Key,
                    Culture = culture,
                    Value = entry.Value.Value ?? string.Empty,
                    Description = entry.Value.Description,
                    GroupName = entry.Value.GroupName ?? GetGroupFromKey(entry.Key),
                    IsActive = entry.Value.IsActive
                });
            }
        }

        return items;
    }

    public async Task<AppTextDictionaryDto> GetDictionaryAsync(string requestedCulture, string defaultCulture, CancellationToken ct)
    {
        await EnsureCultureFilesAsync(new[] { requestedCulture, defaultCulture }, defaultCulture, ct);

        var defaultEntries = (await ReadResourceFileAsync(defaultCulture, ct)).Entries;
        var requestedEntries = string.Equals(requestedCulture, defaultCulture, StringComparison.OrdinalIgnoreCase)
            ? defaultEntries
            : (await ReadResourceFileAsync(requestedCulture, ct)).Entries;

        var keys = defaultEntries.Keys
            .Concat(requestedEntries.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);

        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            if (requestedEntries.TryGetValue(key, out var requested) && requested.IsActive && !string.IsNullOrWhiteSpace(requested.Value))
            {
                dictionary[key] = requested.Value;
                continue;
            }

            if (defaultEntries.TryGetValue(key, out var fallback) && fallback.IsActive && !string.IsNullOrWhiteSpace(fallback.Value))
            {
                dictionary[key] = fallback.Value;
            }
        }

        return new AppTextDictionaryDto
        {
            Culture = requestedCulture,
            DefaultCulture = defaultCulture,
            Texts = dictionary
        };
    }

    public async Task SaveAllAsync(IEnumerable<AppTextTranslationDto> items, IReadOnlyCollection<string> activeCultures, string defaultCulture, CancellationToken ct)
    {
        var normalizedCultures = activeCultures
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (normalizedCultures.Count == 0)
        {
            normalizedCultures.Add(defaultCulture);
        }

        var grouped = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Culture))
            .GroupBy(x => x.Culture.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g
                .GroupBy(x => x.Key.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x =>
                    {
                        var item = x.Last();
                        return new LocalizationEntryFileModel
                        {
                            Value = item.Value.Trim(),
                            Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim(),
                            GroupName = string.IsNullOrWhiteSpace(item.GroupName) ? GetGroupFromKey(item.Key) : item.GroupName.Trim(),
                            IsActive = item.IsActive
                        };
                    },
                    StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

        foreach (var culture in normalizedCultures)
        {
            ct.ThrowIfCancellationRequested();
            if (!grouped.TryGetValue(culture, out var entries))
            {
                if (string.Equals(culture, defaultCulture, StringComparison.OrdinalIgnoreCase))
                {
                    entries = new Dictionary<string, LocalizationEntryFileModel>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    entries = (await ReadResourceFileAsync(culture, ct)).Entries;
                }
            }

            await WriteResourceFileAsync(culture, new LocalizationResourceFileModel { Entries = entries }, ct);
        }
    }

    public async Task EnsureCultureFilesAsync(IReadOnlyCollection<string> activeCultures, string defaultCulture, CancellationToken ct)
    {
        EnsureRootExists();

        var cultures = activeCultures
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Append(defaultCulture)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        LocalizationResourceFileModel? defaultResource = null;
        foreach (var culture in cultures)
        {
            ct.ThrowIfCancellationRequested();
            var path = GetFilePath(culture);
            if (File.Exists(path))
            {
                continue;
            }

            defaultResource ??= await ReadResourceFileAsync(defaultCulture, ct);
            var clone = string.Equals(culture, defaultCulture, StringComparison.OrdinalIgnoreCase)
                ? defaultResource
                : new LocalizationResourceFileModel
                {
                    Entries = defaultResource.Entries.ToDictionary(
                        x => x.Key,
                        x => new LocalizationEntryFileModel
                        {
                            Value = x.Value.Value,
                            Description = x.Value.Description,
                            GroupName = x.Value.GroupName,
                            IsActive = x.Value.IsActive
                        },
                        StringComparer.OrdinalIgnoreCase)
                };

            await WriteResourceFileAsync(culture, clone, ct);
        }
    }

    private async Task<LocalizationResourceFileModel> ReadResourceFileAsync(string culture, CancellationToken ct)
    {
        EnsureRootExists();
        var path = GetFilePath(culture);
        if (!File.Exists(path))
        {
            return new LocalizationResourceFileModel();
        }

        await using var stream = File.OpenRead(path);
        var model = await JsonSerializer.DeserializeAsync<LocalizationResourceFileModel>(stream, JsonOptions, ct)
            ?? new LocalizationResourceFileModel();

        model.Entries ??= new Dictionary<string, LocalizationEntryFileModel>(StringComparer.OrdinalIgnoreCase);
        model.Entries = new Dictionary<string, LocalizationEntryFileModel>(model.Entries, StringComparer.OrdinalIgnoreCase);
        return model;
    }

    private async Task WriteResourceFileAsync(string culture, LocalizationResourceFileModel model, CancellationToken ct)
    {
        EnsureRootExists();
        var path = GetFilePath(culture);
        var normalized = new LocalizationResourceFileModel
        {
            Entries = model.Entries
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase)
        };

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, normalized, JsonOptions, ct);
    }

    private void EnsureRootExists() => Directory.CreateDirectory(GetRootPath());

    private string GetRootPath() => Path.Combine(_environment.ContentRootPath, "App_Data", "Localization");

    private string GetFilePath(string culture) => Path.Combine(GetRootPath(), $"{culture}.json");

    private static string GetGroupFromKey(string key)
    {
        var index = key.IndexOf('.');
        return index > 0 ? key[..index] : "general";
    }

    private sealed class LocalizationResourceFileModel
    {
        public Dictionary<string, LocalizationEntryFileModel> Entries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class LocalizationEntryFileModel
    {
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? GroupName { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
