 namespace RaceDayNova.Api.Models;

/// <summary>Maps to dbo.Users. One table holds both Organisers and Participants (see UserRole).</summary>
public class AppUser
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    /// <summary>PBKDF2 hash - the plain-text password is never stored.</summary>
    public string PasswordHash { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string UserRole { get; set; } = Roles.Participant;
    public DateTime CreatedOn { get; set; }

    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
