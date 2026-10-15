namespace RaceDayNova.Api.Models;

/// <summary>Maps to dbo.Enrolments - links a Participant to an event category.</summary>
public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int EventCategoryId { get; set; }
    public DateTime EnrolledOn { get; set; }
    public string EnrolmentStatus { get; set; } = EnrolmentStatuses.Confirmed;

    public AppUser Participant { get; set; } = null!;
    public EventCategory EventCategory { get; set; } = null!;
    public RaceResult? Result { get; set; }
}
