using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayNova.Api.Data;
using RaceDayNova.Api.Dtos;
using RaceDayNova.Api.Security;

namespace RaceDayNova.Api.Controllers;

/// <summary>The logged-in user's own profile (both roles).</summary>
[Route("api/users")]
[SessionAuthorize]
public class UsersController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher _hasher;

    public UsersController(RaceDayDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    /// <summary>Get the logged-in user's profile.</summary>
    /// <response code="200">The profile of the current session's user.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == CurrentUserId, ct);
        if (user is null)
        {
            HttpContext.Session.Clear();
            return Fail(StatusCodes.Status401Unauthorized, "Session expired", "Your account no longer exists. Please log in again.");
        }
        return Ok(user.ToDto());
    }

    /// <summary>Update the logged-in user's profile.</summary>
    /// <remarks>
    /// Updates name, email and mobile. To change the password supply both <c>currentPassword</c> and
    /// <c>newPassword</c>. The role can never be changed here.
    /// </remarks>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Validation failed, or the current password is wrong / the new password is weak.</response>
    /// <response code="409">The new email belongs to another account.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId, ct);
        if (user is null)
        {
            HttpContext.Session.Clear();
            return Fail(StatusCodes.Status401Unauthorized, "Session expired", "Your account no longer exists. Please log in again.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (email != user.Email && await _db.Users.AnyAsync(u => u.Email == email && u.UserId != user.UserId, ct))
            return Fail(StatusCodes.Status409Conflict, "Email already registered", "Another account already uses this email.");

        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) || !_hasher.Verify(request.CurrentPassword, user.PasswordHash))
                return Fail(StatusCodes.Status400BadRequest, "Current password incorrect", "Supply your correct currentPassword to set a new password.");
            if (!PasswordPolicy.IsValid(request.NewPassword, out var error))
                return Fail(StatusCodes.Status400BadRequest, "Weak password", error);
            user.PasswordHash = _hasher.Hash(request.NewPassword);
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.Mobile = string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile.Trim();

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Fail(StatusCodes.Status409Conflict, "Email already registered", "Another account already uses this email.");
        }
        return Ok(user.ToDto());
    }
}
