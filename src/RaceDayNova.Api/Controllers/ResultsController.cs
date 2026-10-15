using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayNova.Api.Data;
using RaceDayNova.Api.Dtos;
using RaceDayNova.Api.Models;
using RaceDayNova.Api.Security;

namespace RaceDayNova.Api.Controllers;

/// <summary>Race results: Organisers capture them, Participants view their own.</summary>
[Route("api")]
public class ResultsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Capture a Participant's result (Organiser who owns the event).</summary>
    /// <remarks>
    /// One result per enrolment. A Finished result needs a finishTime. The finishing position must be unique within
    /// the event category. If paceSecondsKm is omitted it is calculated from the finish time and the event distance.
    /// </remarks>
    /// <param name="id">The enrolment id (see GET /api/events/{id}/enrolments).</param>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Validation failed, or the enrolment is cancelled.</response>
    /// <response code="403">The enrolment belongs to an event run by a different Organiser.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">This enrolment already has a result, or the position is already taken.</response>
    [HttpPost("enrolments/{id:int}/result")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> Capture(int id, [FromBody] ResultRequest request, CancellationToken ct)
    {
        var enrolment = await _db.Enrolments
            .Include(en => en.Result)
            .Include(en => en.EventCategory).ThenInclude(ec => ec.Event)
            .FirstOrDefaultAsync(en => en.EnrolmentId == id, ct);
        if (enrolment is null) return Fail(StatusCodes.Status404NotFound, "Enrolment not found");

        var ev = enrolment.EventCategory.Event;
        if (ev.OrganiserId != CurrentUserId)
            return Fail(StatusCodes.Status403Forbidden, "Forbidden", "You can only capture results for events that you organise.");
        if (enrolment.EnrolmentStatus == EnrolmentStatuses.Cancelled)
            return Fail(StatusCodes.Status400BadRequest, "Enrolment cancelled", "A result cannot be captured for a cancelled enrolment.");
        if (enrolment.Result is not null)
            return Fail(StatusCodes.Status409Conflict, "Result already exists", "This enrolment already has a result. Use PUT /api/results/{id} to correct it.");

        var result = new RaceResult { EnrolmentId = id, RecordedOn = DateTime.UtcNow };
        var error = await ApplyAsync(result, request, ev.DistanceKm, enrolment.EventCategoryId, ct);
        if (error is not null) return error;

        _db.Results.Add(result);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Fail(StatusCodes.Status409Conflict, "Result already exists", "This enrolment already has a result.");
        }

        var saved = await _db.Results.AsNoTracking().WithDetails().FirstAsync(r => r.ResultId == result.ResultId, ct);
        return Created($"/api/events/{ev.EventId}/results", saved.ToDto());
    }

    /// <summary>Correct an existing result (Organiser who owns the event).</summary>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">The result belongs to an event run by a different Organiser.</response>
    /// <response code="404">Result not found.</response>
    /// <response code="409">The position is already taken by another result in this category.</response>
    [HttpPut("results/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> Update(int id, [FromBody] ResultRequest request, CancellationToken ct)
    {
        var result = await _db.Results.WithDetails().FirstOrDefaultAsync(r => r.ResultId == id, ct);
        if (result is null) return Fail(StatusCodes.Status404NotFound, "Result not found");

        var ev = result.Enrolment.EventCategory.Event;
        if (ev.OrganiserId != CurrentUserId)
            return Fail(StatusCodes.Status403Forbidden, "Forbidden", "You can only correct results for events that you organise.");

        var error = await ApplyAsync(result, request, ev.DistanceKm, result.Enrolment.EventCategoryId, ct);
        if (error is not null) return error;

        await _db.SaveChangesAsync(ct);
        return Ok(result.ToDto());
    }

    /// <summary>View your own results (Participant).</summary>
    /// <response code="200">The logged-in Participant's personal performance history, most recent event first.</response>
    [HttpGet("results/me")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ResultDto>>> GetMine(CancellationToken ct)
    {
        var userId = CurrentUserId;
        var results = await _db.Results.AsNoTracking().WithDetails()
            .Where(r => r.Enrolment.ParticipantId == userId)
            .ToListAsync(ct);
        return Ok(results.OrderByDescending(r => r.Enrolment.EventCategory.Event.EventDate).Select(r => r.ToDto()).ToList());
    }

    /// <summary>View all results for an event you own (Organiser).</summary>
    /// <response code="200">Results ordered by category then finishing position.</response>
    /// <response code="403">The event belongs to a different Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{id:int}/results")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ResultDto>>> GetForEvent(int id, CancellationToken ct)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (ev is null) return Fail(StatusCodes.Status404NotFound, "Event not found");
        if (ev.OrganiserId != CurrentUserId)
            return Fail(StatusCodes.Status403Forbidden, "Forbidden", "You can only view results for events that you organise.");

        var results = await _db.Results.AsNoTracking().WithDetails()
            .Where(r => r.Enrolment.EventCategory.EventId == id)
            .ToListAsync(ct);

        return Ok(results
            .OrderBy(r => r.Enrolment.EventCategory.Category.CategoryName)
            .ThenBy(r => r.PositionNo ?? int.MaxValue)
            .Select(r => r.ToDto())
            .ToList());
    }

    /// <summary>Validates the request and copies it onto the entity. Returns an error response, or null when valid.</summary>
    private async Task<ObjectResult?> ApplyAsync(RaceResult result, ResultRequest request, decimal distanceKm, int eventCategoryId, CancellationToken ct)
    {
        if (!ResultStatuses.TryNormalize(request.ResultStatus, out var status))
            return Fail(StatusCodes.Status400BadRequest, "Invalid result status", "ResultStatus must be Finished, DNF, DNS or DSQ.");

        TimeSpan? finish = null;
        if (request.FinishTime is { } ft)
        {
            if (ft <= TimeSpan.Zero || ft >= TimeSpan.FromHours(24))
                return Fail(StatusCodes.Status400BadRequest, "Invalid finish time", "FinishTime must be greater than 00:00:00 and less than 24:00:00.");
            finish = TimeSpan.FromSeconds(Math.Round(ft.TotalSeconds));
        }

        if (status == ResultStatuses.Finished && finish is null)
            return Fail(StatusCodes.Status400BadRequest, "Finish time required", "A Finished result must include a FinishTime (hh:mm:ss).");

        if (status != ResultStatuses.Finished && request.PositionNo.HasValue)
            return Fail(StatusCodes.Status400BadRequest, "Position not allowed", $"A {status} result cannot have a finishing position.");

        if (request.PositionNo.HasValue)
        {
            var position = request.PositionNo.Value;
            var taken = await _db.Results.AnyAsync(r => r.PositionNo == position && r.ResultId != result.ResultId &&
                                                        r.Enrolment.EventCategoryId == eventCategoryId, ct);
            if (taken)
                return Fail(StatusCodes.Status409Conflict, "Position already taken", $"Another participant in this category already has position {position}.");
        }

        int? pace = request.PaceSecondsKm;
        if (pace is null && finish is not null && distanceKm > 0)
            pace = Math.Max(1, (int)Math.Round(finish.Value.TotalSeconds / (double)distanceKm));

        result.ResultStatus = status;
        result.FinishTime = finish;
        result.PositionNo = request.PositionNo;
        result.PaceSecondsKm = pace;
        return null;
    }
}
