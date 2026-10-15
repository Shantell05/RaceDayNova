using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayNova.Api.Data;
using RaceDayNova.Api.Dtos;
using RaceDayNova.Api.Models;
using RaceDayNova.Api.Security;
using RaceDayNova.Api.Services;

namespace RaceDayNova.Api.Controllers;

/// <summary>Browse events (public) and manage them (Organisers).</summary>
[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private static readonly TimeSpan WeatherFreshness = TimeSpan.FromHours(3);

    private readonly RaceDayDbContext _db;
    private readonly IWeatherProvider _weather;
    private readonly ILogger<EventsController> _logger;

    public EventsController(RaceDayDbContext db, IWeatherProvider weather, ILogger<EventsController> logger)
    {
        _db = db;
        _weather = weather;
        _logger = logger;
    }

    /// <summary>Browse events, with optional filters.</summary>
    /// <remarks>All filters are optional and can be combined. Results are ordered by event date.</remarks>
    /// <param name="eventType">Running, Walking or Cycling (run / walk / cycle also accepted).</param>
    /// <param name="city">Partial, case-insensitive city match.</param>
    /// <param name="province">Partial, case-insensitive province match.</param>
    /// <param name="status">Upcoming, Open, Closed, Completed or Cancelled.</param>
    /// <param name="fromDate">Only events on or after this date (yyyy-MM-dd).</param>
    /// <param name="toDate">Only events on or before this date (yyyy-MM-dd).</param>
    /// <param name="search">Text searched in the event name, venue, city and description.</param>
    /// <param name="organiserId">Only events run by this organiser.</param>
    /// <response code="200">The matching events (empty list when none match).</response>
    /// <response code="400">A filter value is invalid (for example an unknown event type).</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<EventDto>>> GetEvents(
        [FromQuery] string? eventType, [FromQuery] string? city, [FromQuery] string? province,
        [FromQuery] string? status, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] string? search, [FromQuery] int? organiserId, CancellationToken ct)
    {
        var query = _db.Events.AsNoTracking().Include(e => e.Organiser).AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            if (!EventTypes.TryNormalize(eventType, out var type))
                return Fail(StatusCodes.Status400BadRequest, "Invalid event type", "eventType must be Running, Walking or Cycling.");
            query = query.Where(e => e.EventType == type);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!EventStatuses.TryNormalize(status, out var st))
                return Fail(StatusCodes.Status400BadRequest, "Invalid status", "status must be Upcoming, Open, Closed, Completed or Cancelled.");
            query = query.Where(e => e.EventStatus == st);
        }
        if (!string.IsNullOrWhiteSpace(city))
        {
            var c = city.Trim().ToLower();
            query = query.Where(e => e.City.ToLower().Contains(c));
        }
        if (!string.IsNullOrWhiteSpace(province))
        {
            var p = province.Trim().ToLower();
            query = query.Where(e => e.Province.ToLower().Contains(p));
        }
        if (fromDate.HasValue) query = query.Where(e => e.EventDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(e => e.EventDate <= toDate.Value);
        if (organiserId.HasValue) query = query.Where(e => e.OrganiserId == organiserId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(e => e.EventName.ToLower().Contains(s) || e.Venue.ToLower().Contains(s) ||
                                     e.City.ToLower().Contains(s) || (e.Description != null && e.Description.ToLower().Contains(s)));
        }

        var events = await query.OrderBy(e => e.EventDate).ThenBy(e => e.StartTime).ToListAsync(ct);
        return Ok(events.Select(e => e.ToDto()).ToList());
    }

    /// <summary>View one event and the categories it offers.</summary>
    /// <response code="200">The event with its categories, fees and enrolment counts.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> GetEvent(int id, CancellationToken ct)
    {
        var ev = await _db.Events.AsNoTracking().Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (ev is null) return Fail(StatusCodes.Status404NotFound, "Event not found");

        var categories = await _db.EventCategories.AsNoTracking()
            .Where(ec => ec.EventId == id)
            .OrderBy(ec => ec.Category.CategoryName)
            .ToDtos()
            .ToListAsync(ct);

        return Ok(ev.ToDetailDto(categories));
    }

    /// <summary>Create a new event.</summary>
    /// <remarks>The logged-in Organiser becomes the owner of the event. EventStatus defaults to Upcoming.</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] EventRequest request, CancellationToken ct)
    {
        var error = Validate(request, out var type, out var status);
        if (error is not null) return error;

        var ev = new Event { OrganiserId = CurrentUserId, CreatedOn = DateTime.UtcNow };
        Apply(ev, request, type, status);
        _db.Events.Add(ev);
        await _db.SaveChangesAsync(ct);

        var created = await _db.Events.AsNoTracking().Include(e => e.Organiser).FirstAsync(e => e.EventId == ev.EventId, ct);
        return Created($"/api/events/{ev.EventId}", created.ToDto());
    }

    /// <summary>Edit an event you own.</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">The event belongs to a different Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> UpdateEvent(int id, [FromBody] EventRequest request, CancellationToken ct)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (ev is null) return Fail(StatusCodes.Status404NotFound, "Event not found");
        if (ev.OrganiserId != CurrentUserId)
            return Fail(StatusCodes.Status403Forbidden, "Forbidden", "You can only edit events that you organise.");

        var error = Validate(request, out var type, out var status);
        if (error is not null) return error;

        // When the status is omitted on an update, keep the existing status.
        if (string.IsNullOrWhiteSpace(request.EventStatus)) status = ev.EventStatus;

        Apply(ev, request, type, status);
        await _db.SaveChangesAsync(ct);

        var updated = await _db.Events.AsNoTracking().Include(e => e.Organiser).FirstAsync(e => e.EventId == id, ct);
        return Ok(updated.ToDto());
    }

    /// <summary>Delete (or cancel) an event you own.</summary>
    /// <remarks>
    /// An event with no enrolments is deleted outright (its category links and weather snapshots go with it).
    /// An event that already has enrolments is **cancelled** instead - its status becomes Cancelled and all its
    /// enrolments are cancelled - so that participants' history and results are preserved.
    /// </remarks>
    /// <response code="204">Event deleted or cancelled.</response>
    /// <response code="403">The event belongs to a different Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken ct)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (ev is null) return Fail(StatusCodes.Status404NotFound, "Event not found");
        if (ev.OrganiserId != CurrentUserId)
            return Fail(StatusCodes.Status403Forbidden, "Forbidden", "You can only delete events that you organise.");

        var enrolments = await _db.Enrolments.Where(en => en.EventCategory.EventId == id).ToListAsync(ct);
        if (enrolments.Count > 0)
        {
            ev.EventStatus = EventStatuses.Cancelled;
            foreach (var en in enrolments) en.EnrolmentStatus = EnrolmentStatuses.Cancelled;
        }
        else
        {
            _db.Events.Remove(ev);
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Get route information for an event.</summary>
    /// <response code="200">Route reference, distance, venue and start details.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}/route")]
    [ProducesResponseType(typeof(RouteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RouteDto>> GetRoute(int id, CancellationToken ct)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (ev is null) return Fail(StatusCodes.Status404NotFound, "Event not found");
        return Ok(ev.ToRouteDto());
    }

    /// <summary>Get current / cached weather for an event.</summary>
    /// <remarks>
    /// Returns the stored snapshot when it is less than 3 hours old. Otherwise the weather provider (Open-Meteo)
    /// is asked for fresh conditions, which are saved as a new snapshot. If the provider cannot be reached the
    /// most recent stored snapshot is returned (IsCached = true).
    /// </remarks>
    /// <response code="200">A weather reading for the event's city.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="503">No stored snapshot exists and the weather provider is unavailable.</response>
    [HttpGet("{id:int}/weather")]
    [ProducesResponseType(typeof(WeatherDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<WeatherDto>> GetWeather(int id, CancellationToken ct)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id, ct);
        if (ev is null) return Fail(StatusCodes.Status404NotFound, "Event not found");

        var latest = await _db.EventWeatherSnapshots.AsNoTracking()
            .Where(w => w.EventId == id)
            .OrderByDescending(w => w.CheckedAt)
            .FirstOrDefaultAsync(ct);

        if (latest is not null)
        {
            var age = DateTime.UtcNow - latest.CheckedAt;
            if (age >= TimeSpan.Zero && age < WeatherFreshness)
                return Ok(ToWeatherDto(latest, true));
        }

        try
        {
            var reading = await _weather.GetCurrentAsync(ev.City, ev.Province, ct);
            if (reading is not null)
            {
                var snapshot = new EventWeatherSnapshot
                {
                    EventId = id,
                    CheckedAt = DateTime.UtcNow,
                    TemperatureC = reading.TemperatureC,
                    WindKph = reading.WindKph,
                    RainChance = reading.RainChance,
                    Summary = reading.Summary,
                    ProviderName = _weather.Name
                };
                _db.EventWeatherSnapshots.Add(snapshot);
                await _db.SaveChangesAsync(ct);
                return Ok(ToWeatherDto(snapshot, false));
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Weather provider failed for event {EventId}", id);
        }

        if (latest is not null) return Ok(ToWeatherDto(latest, true));

        return Fail(StatusCodes.Status503ServiceUnavailable, "Weather unavailable",
            "No weather data is stored for this event and the weather provider could not be reached.");
    }

    private static WeatherDto ToWeatherDto(EventWeatherSnapshot w, bool cached) => new()
    {
        EventId = w.EventId,
        CheckedAt = w.CheckedAt,
        TemperatureC = w.TemperatureC,
        WindKph = w.WindKph,
        RainChance = w.RainChance,
        Summary = w.Summary,
        ProviderName = w.ProviderName,
        IsCached = cached
    };

    private ObjectResult? Validate(EventRequest request, out string type, out string status)
    {
        status = EventStatuses.Upcoming;
        if (!EventTypes.TryNormalize(request.EventType, out type))
            return Fail(StatusCodes.Status400BadRequest, "Invalid event type", "EventType must be Running, Walking or Cycling (run / walk / cycle also accepted).");

        if (!string.IsNullOrWhiteSpace(request.EventStatus) && !EventStatuses.TryNormalize(request.EventStatus, out status))
            return Fail(StatusCodes.Status400BadRequest, "Invalid event status", "EventStatus must be Upcoming, Open, Closed, Completed or Cancelled.");

        return null;
    }

    private static void Apply(Event ev, EventRequest r, string type, string status)
    {
        ev.EventName = r.EventName.Trim();
        ev.EventType = type;
        ev.EventDate = r.EventDate!.Value;
        ev.StartTime = r.StartTime!.Value;
        ev.Venue = r.Venue.Trim();
        ev.City = r.City.Trim();
        ev.Province = r.Province.Trim();
        ev.DistanceKm = r.DistanceKm;
        ev.Description = r.Description?.Trim();
        ev.RouteReference = r.RouteReference?.Trim();
        ev.EventStatus = status;
    }
}
