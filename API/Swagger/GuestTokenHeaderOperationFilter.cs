using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Swagger
{
    public class GuestTokenHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var allowsAnonymous =
                context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()
                || context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() == true;

            // Special case: account/claim needs X-Guest-Token even though it's authorized
            var isClaimEndpoint =
                context.MethodInfo.Name.Equals("ClaimGuestOrders", StringComparison.OrdinalIgnoreCase);

            if (!allowsAnonymous && !isClaimEndpoint)
                return;

            operation.Parameters ??= new List<OpenApiParameter>();

            if (operation.Parameters.Any(p => p.Name == "X-Guest-Token"))
                return;

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Guest-Token",
                In = ParameterLocation.Header,
                Required = false,
                Description = "Guest token to claim guest orders (e.g. abc123)",
                Schema = new OpenApiSchema { Type = "string" }
            });
        }
    }
}
