using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Authorization;

public sealed class AppFeaturePermissionService : IAppFeaturePermissionService
{
    private readonly AppDbContext _db;

    public AppFeaturePermissionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsAllowedAsync(ClaimsPrincipal user, string featureKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(featureKey))
            return false;

        var allowed = await GetAllowedFeatureKeysAsync(user, ct);
        return allowed.Contains(featureKey);
    }

    public async Task<IReadOnlySet<string>> GetAllowedFeatureKeysAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var roles = user.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (roles.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var saved = await _db.AdminFeaturePermissions
            .AsNoTracking()
            .Where(x => roles.Contains(x.RoleName))
            .ToListAsync(ct);

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in AppFeatures.All)
        {
            var grants = saved
                .Where(x => string.Equals(x.FeatureKey, definition.FeatureKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (grants.Count > 0)
            {
                if (grants.Any(x => x.IsAllowed))
                {
                    result.Add(definition.FeatureKey);
                }
            }
            else if (roles.Any(role => definition.DefaultRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
            {
                result.Add(definition.FeatureKey);
            }
        }

        return result;
    }
}
