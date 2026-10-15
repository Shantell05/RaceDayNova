using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayNova.Api.Data;
using RaceDayNova.Api.Dtos;
using RaceDayNova.Api.Models;
using RaceDayNova.Api.Security;

namespace RaceDayNova.Api.Controllers;

/// <summary>Event enrolments: Participants enter events, Organisers see who entered.</summary>
[Route("api")]
public class EnrolmentsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Enter an event by selecting one of its categories (Participant).</summary>
    /// <remarks>
    /// Records the link between the logged-in Participant, the event and the chosen category.
    /// Rules: the event must not be Closed, Completed or Cancelled; the category must be offered by the event;
    /// a Participant can hold only one active enrolment per event; the category must not be full.
    /// Re-enrolling after cancelling re-activates the earlier enrolment.
    /// </remarks>
    /// <param name="id">The event id.</param>
    /// <response code="201">Enrolment recorded.</response>
    /// <response code="400">Event not open for enrolment, or the category is not offered by the event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Already enrolled in this event, or the category is full.</response>
    [HttpPost("events/{id:int}/enrolments")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Enrol(int id, [FromBody] EnrolRequest request, CancellationToken ct)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (ev is null) return Fail(StatusCodes.Status404NotFound, "Event not found");

        if (ev.EventStatus is EventStatuses.Closed or EventStatuses.Completed or EventStatuses.Cancelled)
            return Fail(StatusCodes.Status400BadRequest, "Event not open", $"This event is {ev.EventStatus} and is not accepting enrolments.");

        var eventCategory = await _db.EventCategories.FirstOrDefaultAsync(ec => ec.EventId == id && ec.CategoryId == request.CategoryId, ct);
        if (eventCategory is null)
            return Fail(StatusCodes.Status400BadRequest, "Category not offered", "The selected category is not offered by this event. See GET /api/events/{id}/categories.");

        var userId = CurrentUserId;
        var existing = await _db.Enrolments
            .Where(en => en.ParticipantId == userId && en.EventCategory.EventId == id)
            .ToListAsync(ct);

        if (existing.Any(en => en.EnrolmentStatus != EnrolmentStatuses.Cancelled))
            return Fail(StatusCodes.Status409Conflict, "Already enrolled", "You already have an active enrolment for this event. Cancel it first to change category.");

        if (eventCategory.PlacesAvailable.HasValue)
        {
            var taken = await _db.Enrolments.CountAsync(en => en.EventCategoryId == eventCategory.EventCategoryId && en.EnrolmentStatus != EnrolmentStatuses.Cancelled, ct);
            if (taken >= eventCategory.PlacesAvailable.Value)
                return Fail(StatusCodes.Status409Conflict, "Category full", "There are no places left in this category.");
        }

        // The table has UNIQUE(ParticipantId, EventCategoryId), so a cancelled enrolment is re-activated, not duplicated.
        var enrolment = existing.FirstOrDefault(en => en.EventCategoryId == eventCategory.EventCategoryId);
        if (enrolment is null)
        {
            enrolment = new Enrolment
            {
                ParticipantId = userId,
                EventCategoryId = eventCategory.EventCategoryId,
                EnrolledOn = DateTime.UtcNow,
                EnrolmentStatus = EnrolmentStatuses.Confirmed
            };
            _db.Enrolments.Add(enrolment);
        }
        else
        {
            enrolment.EnrolmentStatus = EnrolmentStatuses.Confirmed;
            enrolment.EnrolledOn = DateTime.UtcNow;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Fail(StatusCodes.Status409Conflict, "Already enrolled", "You are already enrolled in this category.");
        }

        var saved = await _db.Enrolments.AsNoTracking().WithDetails().FirstAsync(en => en.EnrolmentId == enrolment.EnrolmentId, ct);
        return Created("/api/enrolments/me", saved.ToDto());
    }

    /// <summary>View your own enrolments (Participant).</summary>
    /// <response code="200">All of the logged-in Participant's enrolments, newest first.</response>
    [HttpGet("enrolments/me")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetMine(CancellationToken ct)
    {
        var userId = CurrentUserId;
        var enrolments = await _db.Enrolments.AsNoTracking().WithDetails()
            .Where(en => en.ParticipantId == userId)
            .OrderByDescending(en => en.EnrolledOn)
            .ToListAsync(ct);
        return Ok(enrolments.Select(en => en.ToDto()).ToList());
    }

    /// <summary>View all enrolments for an event you own (Organiser).</summary>
    /// <response code="200">Every enrolment (including cancelled ones) for the event.</response>
    /// <response code="403">The event belongs to a different Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{id:int}/enrolments")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetForEvent(int id, CancellationToken ct)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (ev is null) return Fail(StatusCodes.Status404NotFound, "Event not found");
        if (ev.OrganiserId != CurrentUserId)
            return Fail(StatusCodes.Status403Forbidden, "Forbidden", "You can only view enrolments for events that you organise.");

        var enrolments = await _db.Enrolments.AsNoTracking().WithDetails()
            .Where(en => en.EventCategory.EventId == id)
            .OrderBy(en => en.EnrolledOn)
            .ToListAsync(ct);
        return Ok(enrolments.Select(en => en.ToDto()).ToList());
    }

    /// <summary>Cancel your own enrolment (Participant).</summary>
    /// <remarks>The enrolment is marked Cancelled (not erased) so history is kept. Not possible once a result exists.</remarks>
    /// <response code="204">Enrolment cancelled (also returned if it was already cancelled).</response>
    /// <response code="403">The enrolment belongs to another Participant.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been recorded for this enrolment.</response>
    [HttpDelete("enrolments/{id:int}")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var enrolment = await _db.Enrolments.Include(en => en.Result).FirstOrDefaultAsync(en => en.EnrolmentId == id, ct);
        if (enrolment is null) return Fail(StatusCodes.Status404NotFound, "Enrolment not found");
        if (enrolment.ParticipantId != CurrentUserId)
            return Fail(StatusCodes.Status403Forbidden, "Forbidden", "You can only cancel your own enrolments.");
        if (enrolment.Result is not null)
            return Fail(StatusCodes.Status409Conflict, "Result recorded", "A result has already been recorded, so this enrolment can no longer be cancelled.");

        if (enrolment.EnrolmentStatus != EnrolmentStatuses.Cancelled)
        {
            enrolment.EnrolmentStatus = EnrolmentStatuses.Cancelled;
            await _db.SaveChangesAsync(ct);
        }
        return NoContent();
    }
}
