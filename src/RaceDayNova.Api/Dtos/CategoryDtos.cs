using System.ComponentModel.DataAnnotations;

namespace RaceDayNova.Api.Dtos;

/// <summary>Fields used to create or update a reusable category.</summary>
public class CategoryRequest
{
    /// <example>Under 20</example>
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Minimum age (optional, for age categories).</summary>
    [Range(0, 120)]
    public short? AgeFrom { get; set; }

    /// <summary>Maximum age (optional). Must be greater than or equal to AgeFrom.</summary>
    [Range(0, 120)]
    public short? AgeTo { get; set; }

    /// <example>Distance category: 21km</example>
    [StringLength(250)]
    public string? CategoryNote { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public short? AgeFrom { get; set; }
    public short? AgeTo { get; set; }
    public string? CategoryNote { get; set; }
}

/// <summary>Attach an existing category to an event.</summary>
public class AddEventCategoryRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a valid category id.")]
    public int CategoryId { get; set; }

    /// <summary>Entry fee in rand (0 or more).</summary>
    [Range(0, 1000000)]
    public decimal EntryFee { get; set; }

    /// <summary>Capacity for this category in this event. Leave empty for unlimited.</summary>
    [Range(1, int.MaxValue)]
    public int? PlacesAvailable { get; set; }
}

/// <summary>A category as offered by one specific event.</summary>
public class EventCategoryDto
{
    public int EventCategoryId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public short? AgeFrom { get; set; }
    public short? AgeTo { get; set; }
    public string? CategoryNote { get; set; }
    public decimal EntryFee { get; set; }
    public int? PlacesAvailable { get; set; }
    /// <summary>Number of active (non-cancelled) enrolments.</summary>
    public int EnrolmentCount { get; set; }
}
