using System.ComponentModel.DataAnnotations;

namespace RaceDayNova.Api.Dtos;

/// <summary>Fields used to capture or correct a result.</summary>
public class ResultRequest
{
    /// <summary>Finishing position (1 or more). Must be unique within the event category.</summary>
    /// <example>12</example>
    [Range(1, int.MaxValue)]
    public int? PositionNo { get; set; }

    /// <summary>Finish time as hh:mm:ss. Required when ResultStatus is Finished.</summary>
    /// <example>00:52:30</example>
    public TimeSpan? FinishTime { get; set; }

    /// <summary>Pace in seconds per km. Calculated automatically from FinishTime and the event distance when omitted.</summary>
    [Range(1, int.MaxValue)]
    public int? PaceSecondsKm { get; set; }

    /// <summary>Finished (default), DNF, DNS or DSQ.</summary>
    /// <example>Finished</example>
    public string ResultStatus { get; set; } = "Finished";
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? PositionNo { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? PaceSecondsKm { get; set; }
    public string ResultStatus { get; set; } = string.Empty;
    public DateTime RecordedOn { get; set; }
}
