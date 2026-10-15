using Microsoft.OpenApi.Models;
using RaceDayNova.Api.Security;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDayNova.Api.Swagger;

/// <summary>
/// Adds an "Access" line to every endpoint in the Swagger UI (public / any logged-in user / specific role)
/// and documents the 401 and 403 responses that session authorisation can return.
/// </summary>
public class SessionAccessOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var attributes = context.MethodInfo.GetCustomAttributes(true).OfType<SessionAuthorizeAttribute>()
            .Concat(context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<SessionAuthorizeAttribute>()
                    ?? Enumerable.Empty<SessionAuthorizeAttribute>())
            .ToList();

        string access;
        if (attributes.Count == 0)
        {
            access = "**Access:** Public - no login required.";
        }
        else
        {
            var roles = attributes.SelectMany(a => a.Roles).Distinct().ToList();
            access = roles.Count == 0
                ? "**Access:** any logged-in user (Organiser or Participant). Log in first with POST /api/auth/login."
                : $"**Access:** {string.Join(" / ", roles)} only. Log in first with POST /api/auth/login.";

            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Not logged in." });
            if (roles.Count > 0)
                operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Logged in, but with the wrong role (or not the owner of the resource)." });
        }

        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? access
            : $"{operation.Description}\n\n{access}";
    }
}
