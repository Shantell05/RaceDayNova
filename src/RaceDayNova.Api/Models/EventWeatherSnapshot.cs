namespace RaceDayNova.Api.Models;

/// <summary>Maps to dbo.EventWeatherSnapshots - cached weather readings for an event.</summary>
public class EventWeatherSnapshot
{
    public int WeatherSnapshotId { get; set; }
    public int EventId { get; set; }
    public DateTime CheckedAt { get; set; }
    public decimal? TemperatureC { get; set; }
    public decimal? WindKph { get; set; }
    public decimal? RainChance { get; set; }
    public string? Summary { get; set; }
    public string? ProviderName { get; set; }

    public Event Event { get; set; } = null!;
}
