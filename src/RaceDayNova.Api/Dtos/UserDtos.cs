using System.ComponentModel.DataAnnotations;

namespace RaceDayNova.Api.Dtos;

/// <summary>Public profile of a user. The password hash is never exposed.</summary>
public class UserDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}

/// <summary>Profile fields a user may change. The role cannot be changed.</summary>
public class UpdateProfileRequest
{
    [Required, StringLength(60)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^\+?[0-9\s\-()]{7,20}$", ErrorMessage = "Mobile must be a valid phone number (7-20 digits, optional leading +).")]
    public string? Mobile { get; set; }

    /// <summary>Only needed when changing the password. Must match the current password.</summary>
    public string? CurrentPassword { get; set; }

    /// <summary>Optional new password (same rules as registration).</summary>
    public string? NewPassword { get; set; }
}
