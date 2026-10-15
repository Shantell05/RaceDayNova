using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayNova.Api.Data;
using RaceDayNova.Api.Dtos;
using RaceDayNova.Api.Models;
using RaceDayNova.Api.Security;

namespace RaceDayNova.Api.Controllers;

/// <summary>Registration, login and logout. Login creates a server-side session holding the user id and role.</summary>
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private static readonly string DummyHash = new Pbkdf2PasswordHasher().Hash("dummy-password-for-timing");

    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _config;

    public AuthController(RaceDayDbContext db, IPasswordHasher hasher, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
    }

    /// <summary>Register a new account (Participant or Organiser).</summary>
    /// <remarks>
    /// Anyone may register as a **Participant**. Registering as an **Organiser** also requires the
    /// <c>organiserInviteCode</c> held in server configuration, so a participant cannot simply promote themselves.
    /// The password is hashed with salted PBKDF2 before it is stored. Registering does not log you in.
    /// </remarks>
    /// <response code="201">Account created. The body is the new user's profile.</response>
    /// <response code="400">Validation failed (missing fields, bad email, weak password, unknown role).</response>
    /// <response code="403">Organiser registration without a valid invite code.</response>
    /// <response code="409">An account with that email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (!Roles.TryNormalize(request.Role, out var role))
            return Fail(StatusCodes.Status400BadRequest, "Invalid role", "Role must be either 'Participant' or 'Organiser'.");

        if (!PasswordPolicy.IsValid(request.Password, out var passwordError))
            return Fail(StatusCodes.Status400BadRequest, "Weak password", passwordError);

        if (role == Roles.Organiser)
        {
            var expected = _config["Registration:OrganiserInviteCode"];
            if (string.IsNullOrWhiteSpace(expected))
                return Fail(StatusCodes.Status403Forbidden, "Organiser registration disabled",
                    "Organiser registration is not enabled on this server.");

            var supplied = request.OrganiserInviteCode ?? string.Empty;
            var match = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(expected));
            if (!match)
                return Fail(StatusCodes.Status403Forbidden, "Invalid organiser invite code",
                    "A valid organiserInviteCode is required to register as an Organiser.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Fail(StatusCodes.Status409Conflict, "Email already registered", "An account with this email already exists.");

        var user = new AppUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            Mobile = string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile.Trim(),
            UserRole = role,
            CreatedOn = DateTime.UtcNow
        };

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two simultaneous registrations with the same email: the UNIQUE index caught it.
            return Fail(StatusCodes.Status409Conflict, "Email already registered", "An account with this email already exists.");
        }

        return Created("/api/users/me", user.ToDto());
    }

    /// <summary>Log in with email and password.</summary>
    /// <remarks>
    /// On success a server-side session is created that stores the user's id and role, and a session cookie is
    /// returned. The Swagger UI (and any browser) sends the cookie automatically on later requests.
    /// </remarks>
    /// <response code="200">Logged in. The body contains the user's profile.</response>
    /// <response code="401">Email or password is incorrect.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            _hasher.Verify(request.Password, DummyHash); // keep timing similar for unknown emails
            return Fail(StatusCodes.Status401Unauthorized, "Invalid credentials", "Email or password is incorrect.");
        }

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            return Fail(StatusCodes.Status401Unauthorized, "Invalid credentials", "Email or password is incorrect.");

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.UserRole, user.UserRole);

        return Ok(new LoginResponse { Message = $"Logged in as {user.UserRole}.", User = user.ToDto() });
    }

    /// <summary>Log out and destroy the current session.</summary>
    /// <response code="204">Session cleared (also returned when nobody was logged in).</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }
}
