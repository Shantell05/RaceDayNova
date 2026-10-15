namespace RaceDayNova.Api.Models;

/// <summary>Case-insensitive matching helper used by the constant classes below.</summary>
internal static class Normaliser
{
    public static bool TryMatch(string? value, IEnumerable<string> allowed, out string match)
    {
        match = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var found = allowed.FirstOrDefault(a => string.Equals(a, value.Trim(), StringComparison.OrdinalIgnoreCase));
        if (found is null) return false;
        match = found;
        return true;
    }
}

/// <summary>The two roles in the system (matches CK_Users_UserRole).</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static bool TryNormalize(string? value, out string role) =>
        Normaliser.TryMatch(value, new[] { Organiser, Participant }, out role);
}

/// <summary>Event types (matches CK_Events_EventType). "run", "walk" and "cycle" are accepted as aliases.</summary>
public static class EventTypes
{
    public const string Running = "Running";
    public const string Walking = "Walking";
    public const string Cycling = "Cycling";

    public static bool TryNormalize(string? value, out string type)
    {
        var mapped = value?.Trim().ToLowerInvariant() switch
        {
            "run" or "running" => Running,
            "walk" or "walking" => Walking,
            "cycle" or "cycling" => Cycling,
            _ => null
        };
        type = mapped ?? string.Empty;
        return mapped is not null;
    }
}

/// <summary>Event lifecycle (matches CK_Events_Status).</summary>
public static class EventStatuses
{
    public const string Upcoming = "Upcoming";
    public const string Open = "Open";
    public const string Closed = "Closed";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static bool TryNormalize(string? value, out string status) =>
        Normaliser.TryMatch(value, new[] { Upcoming, Open, Closed, Completed, Cancelled }, out status);
}

/// <summary>Enrolment status (matches CK_Enrolments_Status).</summary>
public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
}

/// <summary>Result status (matches CK_Results_Status).</summary>
public static class ResultStatuses
{
    public const string Finished = "Finished";
    public const string DNF = "DNF";
    public const string DNS = "DNS";
    public const string DSQ = "DSQ";

    public static bool TryNormalize(string? value, out string status) =>
        Normaliser.TryMatch(value, new[] { Finished, DNF, DNS, DSQ }, out status);
}
