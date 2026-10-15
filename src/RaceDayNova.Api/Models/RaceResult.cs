namespace RaceDayNova.Api.Models;

/// <summary>Maps to dbo.Results - zero or one result per enrolment.</summary>
public class RaceResult
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int? PositionNo { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? PaceSecondsKm { get; set; }
    public string ResultStatus { get; set; } = ResultStatuses.Finished;
    public DateTime RecordedOn { get; set; }

    public Enrolment Enrolment { get; set; } = null!;
}
