using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayNova.Api.Data;
using RaceDayNova.Api.Dtos;
using RaceDayNova.Api.Models;
using RaceDayNova.Api.Security;

namespace RaceDayNova.Api.Controllers;

/// <summary>The categories offered by a particular event (the EventCategories bridge).</summary>
[Route("api/events/{id:int}/categories")]
public class EventCategoriesController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventCategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>List the categories offered by an event.</summary>
    /// <response code="200">The event's categories with fee, capacity and current enrolment count.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EventCategoryDto>>> GetForEvent(int id, CancellationToken ct)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == id, ct))
            return Fail(StatusCodes.Status404NotFound, "Event not found");

        var categories = await _db.EventCategories.AsNoTracking()
            .Where(ec => ec.EventId == id)
            .OrderBy(ec => ec.Category.CategoryName)
            .ToDtos()
            .ToListAsync(ct);
        return Ok(categories);
    }

    /// <summary>Add a category to an event you own.</summary>
    /// <remarks>Body: categoryId (an existing category), entryFee and an optional placesAvailable capacity.</remarks>
    /// <response code="201">Category added to the event.</response>
    /// <response code="400">Validation failed, or the categoryId does not exist.</response>
    /// <response code="403">The event belongs to a different Organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">The event already offers this category.</response>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventCategoryDto>> Add(int id, [FromBody] AddEventCategoryRequest request, CancellationToken ct)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (ev is null) return Fail(StatusCodes.Status404NotFound, "Event not found");
        if (ev.OrganiserId != CurrentUserId)
            return Fail(StatusCodes.Status403Forbidden, "Forbidden", "You can only manage categories for events that you organise.");

        if (!await _db.Categories.AnyAsync(c => c.CategoryId == request.CategoryId, ct))
            return Fail(StatusCodes.Status400BadRequest, "Unknown category", $"Category {request.CategoryId} does not exist. Create it first with POST /api/categories.");

        if (await _db.EventCategories.AnyAsync(ec => ec.EventId == id && ec.CategoryId == request.CategoryId, ct))
            return Fail(StatusCodes.Status409Conflict, "Category already added", "This event already offers that category.");

        var link = new EventCategory
        {
            EventId = id,
            CategoryId = request.CategoryId,
            EntryFee = request.EntryFee,
            PlacesAvailable = request.PlacesAvailable
        };
        _db.EventCategories.Add(link);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Fail(StatusCodes.Status409Conflict, "Category already added", "This event already offers that category.");
        }

        var dto = await _db.EventCategories.AsNoTracking()
            .Where(ec => ec.EventCategoryId == link.EventCategoryId)
            .ToDtos()
            .FirstAsync(ct);
        return Created($"/api/events/{id}/categories", dto);
    }

    /// <summary>Remove a category from an event you own.</summary>
    /// <remarks>Not allowed once participants have enrolled in that category (409).</remarks>
    /// <param name="id">The event id.</param>
    /// <param name="categoryId">The CategoryId to remove from the event.</param>
    /// <response code="204">Category removed from the event.</response>
    /// <response code="403">The event belongs to a different Organiser.</response>
    /// <response code="404">Event not found, or it does not offer that category.</response>
    /// <response code="409">Participants are already enrolled in this category.</response>
    [HttpDelete("{categoryId:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remove(int id, int categoryId, CancellationToken ct)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (ev is null) return Fail(StatusCodes.Status404NotFound, "Event not found");
        if (ev.OrganiserId != CurrentUserId)
            return Fail(StatusCodes.Status403Forbidden, "Forbidden", "You can only manage categories for events that you organise.");

        var link = await _db.EventCategories.FirstOrDefaultAsync(ec => ec.EventId == id && ec.CategoryId == categoryId, ct);
        if (link is null) return Fail(StatusCodes.Status404NotFound, "Category not offered", "This event does not offer that category.");

        if (await _db.Enrolments.AnyAsync(en => en.EventCategoryId == link.EventCategoryId, ct))
            return Fail(StatusCodes.Status409Conflict, "Category has enrolments", "Participants have already enrolled in this category, so it cannot be removed.");

        _db.EventCategories.Remove(link);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
