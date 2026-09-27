using System.Security.Claims;

namespace API.Authorization;

public interface IAppFeaturePermissionService
{
    Task<bool> IsAllowedAsync(ClaimsPrincipal user, string featureKey, CancellationToken ct);
    Task<IReadOnlySet<string>> GetAllowedFeatureKeysAsync(ClaimsPrincipal user, CancellationToken ct);
}
