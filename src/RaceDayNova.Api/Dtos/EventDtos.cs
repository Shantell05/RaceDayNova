using System.ComponentModel.DataAnnotations;

namespace RaceDayNova.Api.Dtos;

/// <summary>Fields used to create or update an event.</summary>
public class EventRequest
{
    /// <example>Lowveld Sunrise 10K</example>
    [Required, StringLength(160)]
    public string EventName { get; set; } = string.Empty;

    /// <summary>Running, Walking or Cycling (the aliases run / walk / cycle are also accepted).</summary>
    /// <example>Running</example>
    [Required]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Format yyyy-MM-dd.</summary>
    /// <example>2026-12-05</example>
    [Required]
    public DateOnly? EventDate { get; set; }

    /// <summary>Format HH:mm:ss.</summary>
    /// <example>06:30:00</example>
    [Required]
    public TimeOnly? StartTime { get; set; }

    /// <example>Civic Sports Ground</example>
    [Required, StringLength(160)]
    public string Venue { get; set; } = string.Empty;

    /// <example>Mbombela</example>
    [Required, StringLength(80)]
    public string City { get; set; } = string.Empty;

    /// <example>Mpumalanga</example>
    [Required, StringLength(80)]
    public string Province { get; set; } = string.Empty;

    /// <summary>Distance in kilometres (must be greater than 0).</summary>
    /// <example>10</example>
    [Range(0.01, 100000, ErrorMessage = "DistanceKm must be greater than 0.")]
    public decimal DistanceKm { get; set; }

    [StringLength(800)]
    public string? Description { get; set; }

    [StringLength(400)]
    public string? RouteReference { get; set; }

    /// <summary>Upcoming (default), Open, Closed, Completed or Cancelled.</summary>
    public string? EventStatus { get; set; }
}

/// <summary>An event as returned by the API.</summary>
public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public string Venue { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string? Description { get; set; }
    public string? RouteReference { get; set; }
    public string EventStatus { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}

/// <summary>An event together with the categories it offers.</summary>
public class EventDetailDto : EventDto
{
    public List<EventCategoryDto> Categories { get; set; } = new();
}

/// <summary>Route information for an event.</summary>
public class RouteDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? RouteReference { get; set; }
    public decimal DistanceKm { get; set; }
    public string Venue { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
}

/// <summary>Weather for an event's location.</summary>
public class WeatherDto
{
    public int EventId { get; set; }
    public DateTime CheckedAt { get; set; }
    public decimal? TemperatureC { get; set; }
    public decimal? WindKph { get; set; }
    public decimal? RainChance { get; set; }
    public string? Summary { get; set; }
    public string? ProviderName { get; set; }
    /// <summary>True when the reading came from a stored snapshot rather than a fresh provider call.</summary>
    public bool IsCached { get; set; }
}
