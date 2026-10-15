 namespace RaceDayNova.Api.Models;

/// <summary>Maps to dbo.Events.</summary>
public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = EventTypes.Running;
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public string Venue { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string? Description { get; set; }
    public string? RouteReference { get; set; }
    public string EventStatus { get; set; } = EventStatuses.Upcoming;
    public DateTime CreatedOn { get; set; }

    public AppUser Organiser { get; set; } = null!;
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
    public ICollection<EventWeatherSnapshot> WeatherSnapshots { get; set; } = new List<EventWeatherSnapshot>();
}
