namespace RaceDayNova.Api.Security;

/// <summary>Simple password rules enforced on registration and password change.</summary>
public static class PasswordPolicy
{
    public static bool IsValid(string? password, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrEmpty(password) || password.Length < 8)
        {
            error = "Password must be at least 8 characters long.";
            return false;
        }
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            error = "Password must contain at least one letter and one digit.";
            return false;
        }
        return true;
    }
}
