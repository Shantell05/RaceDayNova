namespace RaceDayNova.Api.Models;

/// <summary>Maps to dbo.Categories (reusable age / distance category definitions).</summary>
public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public short? AgeFrom { get; set; }
    public short? AgeTo { get; set; }
    public string? CategoryNote { get; set; }

    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
}
