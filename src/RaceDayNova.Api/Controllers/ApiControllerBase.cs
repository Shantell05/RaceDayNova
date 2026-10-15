using Microsoft.AspNetCore.Mvc;
using RaceDayNova.Api.Security;

namespace RaceDayNova.Api.Controllers;

/// <summary>Shared helpers for all API controllers.</summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Id of the logged-in user (read from the server-side session). 0 when nobody is logged in.</summary>
    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    /// <summary>Returns an RFC 7807 problem response with the given status.</summary>
    protected ObjectResult Fail(int status, string title, string? detail = null) =>
        Problem(statusCode: status, title: title, detail: detail);
}
