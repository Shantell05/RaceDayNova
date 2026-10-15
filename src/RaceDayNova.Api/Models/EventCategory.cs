namespace RaceDayNova.Api.Models;

/// <summary>Maps to dbo.EventCategories - the bridge that resolves the Events/Categories many-to-many.</summary>
public class EventCategory
{
    public int EventCategoryId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public decimal EntryFee { get; set; }
    /// <summary>Capacity of this category for the event. NULL means unlimited.</summary>
    public int? PlacesAvailable { get; set; }

    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
