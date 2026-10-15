using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayNova.Api.Security;

/// <summary>Keys used to store the authenticated user inside the server-side session.</summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string UserRole = "UserRole";
}

/// <summary>
/// Session-based authorisation. Reads the user id and role from the server-side session that is created at login.
/// No roles listed = any logged-in user. 401 when not logged in, 403 when logged in with the wrong role.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string[] Roles { get; }

    public SessionAuthorizeAttribute(params string[] roles)
    {
        Roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.UserRole);

        if (userId is null || string.IsNullOrEmpty(role))
        {
            context.Result = Problem(StatusCodes.Status401Unauthorized, "Authentication required",
                "You must log in (POST /api/auth/login) before using this endpoint.");
            return;
        }

        if (Roles.Length > 0 && !Roles.Contains(role))
        {
            context.Result = Problem(StatusCodes.Status403Forbidden, "Forbidden",
                $"This endpoint is restricted to: {string.Join(", ", Roles)}. You are logged in as {role}.");
        }
    }

    private static ObjectResult Problem(int status, string title, string detail)
    {
        var result = new ObjectResult(new ProblemDetails { Status = status, Title = title, Detail = detail })
        {
            StatusCode = status
        };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
