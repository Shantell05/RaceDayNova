using System.ComponentModel.DataAnnotations;

namespace RaceDayNova.Api.Dtos;

/// <summary>Choose the category to enter.</summary>
public class EnrolRequest
{
    /// <summary>The CategoryId of a category offered by the event (see GET /api/events/{id}/categories).</summary>
    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a valid category id.")]
    public int CategoryId { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int EventCategoryId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }
    public DateTime EnrolledOn { get; set; }
    public string EnrolmentStatus { get; set; } = string.Empty;
    public bool HasResult { get; set; }
}
