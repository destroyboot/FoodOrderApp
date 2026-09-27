using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class AppFeatureAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _featureKey;

    public AppFeatureAuthorizeAttribute(string featureKey)
    {
        _featureKey = featureKey;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            return;

        var permissions = context.HttpContext.RequestServices.GetRequiredService<IAppFeaturePermissionService>();
        var allowed = await permissions.IsAllowedAsync(context.HttpContext.User, _featureKey, context.HttpContext.RequestAborted);
        if (!allowed)
        {
            context.Result = new ForbidResult();
        }
    }
}
