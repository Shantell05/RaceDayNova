using System.ComponentModel.DataAnnotations;

namespace RaceDayNova.Api.Dtos;

/// <summary>Details needed to create an account.</summary>
public class RegisterRequest
{
    /// <example>Lebo</example>
    [Required, StringLength(60)]
    public string FirstName { get; set; } = string.Empty;

    /// <example>Molefe</example>
    [Required, StringLength(60)]
    public string LastName { get; set; } = string.Empty;

    /// <example>lebo@example.com</example>
    [Required, EmailAddress, StringLength(180)]
    public string Email { get; set; } = string.Empty;

    /// <summary>At least 8 characters, containing at least one letter and one digit.</summary>
    /// <example>Password123!</example>
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <example>+27821234567</example>
    [RegularExpression(@"^\+?[0-9\s\-()]{7,20}$", ErrorMessage = "Mobile must be a valid phone number (7-20 digits, optional leading +).")]
    public string? Mobile { get; set; }

    /// <summary>Either "Participant" or "Organiser".</summary>
    /// <example>Participant</example>
    [Required]
    public string Role { get; set; } = "Participant";

    /// <summary>
    /// Required only when Role is "Organiser". Organiser accounts can create events and capture results, so
    /// they are protected by an invite code held in server configuration (Registration:OrganiserInviteCode).
    /// </summary>
    public string? OrganiserInviteCode { get; set; }
}

/// <summary>Credentials used to log in.</summary>
public class LoginRequest
{
    /// <example>lebo.molefe@racedaynova.co.za</example>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <example>Password123!</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Returned after a successful login. The session itself is carried by a cookie.</summary>
public class LoginResponse
{
    public string Message { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}
