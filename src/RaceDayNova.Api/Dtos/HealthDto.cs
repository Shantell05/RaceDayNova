namespace RaceDayNova.Api.Dtos;

public class HealthDto
{
    public string Status { get; set; } = "Healthy";
    public string Database { get; set; } = "Connected";
    public DateTime TimestampUtc { get; set; }
}
