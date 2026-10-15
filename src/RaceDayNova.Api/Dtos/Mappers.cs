using Microsoft.EntityFrameworkCore;
using RaceDayNova.Api.Models;

namespace RaceDayNova.Api.Dtos;

/// <summary>Entity -> DTO mapping and the Include chains the mappers rely on.</summary>
public static class Mappers
{
    public static UserDto ToDto(this AppUser u) => new()
    {
        UserId = u.UserId,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Email = u.Email,
        Mobile = u.Mobile,
        UserRole = u.UserRole,
        CreatedOn = u.CreatedOn
    };

    private static void Fill(EventDto d, Event e)
    {
        d.EventId = e.EventId;
        d.OrganiserId = e.OrganiserId;
        d.OrganiserName = e.Organiser is null ? string.Empty : $"{e.Organiser.FirstName} {e.Organiser.LastName}";
        d.EventName = e.EventName;
        d.EventType = e.EventType;
        d.EventDate = e.EventDate;
        d.StartTime = e.StartTime;
        d.Venue = e.Venue;
        d.City = e.City;
        d.Province = e.Province;
        d.DistanceKm = e.DistanceKm;
        d.Description = e.Description;
        d.RouteReference = e.RouteReference;
        d.EventStatus = e.EventStatus;
        d.CreatedOn = e.CreatedOn;
    }

    /// <summary>Requires Event.Organiser to be loaded.</summary>
    public static EventDto ToDto(this Event e)
    {
        var d = new EventDto();
        Fill(d, e);
        return d;
    }

    public static EventDetailDto ToDetailDto(this Event e, List<EventCategoryDto> categories)
    {
        var d = new EventDetailDto { Categories = categories };
        Fill(d, e);
        return d;
    }

    public static RouteDto ToRouteDto(this Event e) => new()
    {
        EventId = e.EventId,
        EventName = e.EventName,
        RouteReference = e.RouteReference,
        DistanceKm = e.DistanceKm,
        Venue = e.Venue,
        City = e.City,
        Province = e.Province,
        EventDate = e.EventDate,
        StartTime = e.StartTime
    };

    public static CategoryDto ToDto(this Category c) => new()
    {
        CategoryId = c.CategoryId,
        CategoryName = c.CategoryName,
        AgeFrom = c.AgeFrom,
        AgeTo = c.AgeTo,
        CategoryNote = c.CategoryNote
    };

    /// <summary>Projection (translated to SQL) for the categories offered by an event.</summary>
    public static IQueryable<EventCategoryDto> ToDtos(this IQueryable<EventCategory> query) =>
        query.Select(ec => new EventCategoryDto
        {
            EventCategoryId = ec.EventCategoryId,
            EventId = ec.EventId,
            CategoryId = ec.CategoryId,
            CategoryName = ec.Category.CategoryName,
            AgeFrom = ec.Category.AgeFrom,
            AgeTo = ec.Category.AgeTo,
            CategoryNote = ec.Category.CategoryNote,
            EntryFee = ec.EntryFee,
            PlacesAvailable = ec.PlacesAvailable,
            EnrolmentCount = ec.Enrolments.Count(en => en.EnrolmentStatus != EnrolmentStatuses.Cancelled)
        });

    public static IQueryable<Enrolment> WithDetails(this IQueryable<Enrolment> query) =>
        query.Include(e => e.Participant)
             .Include(e => e.Result)
             .Include(e => e.EventCategory).ThenInclude(ec => ec.Event)
             .Include(e => e.EventCategory).ThenInclude(ec => ec.Category);

    public static EnrolmentDto ToDto(this Enrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId,
        ParticipantId = e.ParticipantId,
        ParticipantName = $"{e.Participant.FirstName} {e.Participant.LastName}",
        ParticipantEmail = e.Participant.Email,
        EventId = e.EventCategory.EventId,
        EventName = e.EventCategory.Event.EventName,
        EventDate = e.EventCategory.Event.EventDate,
        EventCategoryId = e.EventCategoryId,
        CategoryId = e.EventCategory.CategoryId,
        CategoryName = e.EventCategory.Category.CategoryName,
        EntryFee = e.EventCategory.EntryFee,
        EnrolledOn = e.EnrolledOn,
        EnrolmentStatus = e.EnrolmentStatus,
        HasResult = e.Result is not null
    };

    public static IQueryable<RaceResult> WithDetails(this IQueryable<RaceResult> query) =>
        query.Include(r => r.Enrolment).ThenInclude(en => en.Participant)
             .Include(r => r.Enrolment).ThenInclude(en => en.EventCategory).ThenInclude(ec => ec.Event)
             .Include(r => r.Enrolment).ThenInclude(en => en.EventCategory).ThenInclude(ec => ec.Category);

    public static ResultDto ToDto(this RaceResult r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantId = r.Enrolment.ParticipantId,
        ParticipantName = $"{r.Enrolment.Participant.FirstName} {r.Enrolment.Participant.LastName}",
        EventId = r.Enrolment.EventCategory.EventId,
        EventName = r.Enrolment.EventCategory.Event.EventName,
        EventDate = r.Enrolment.EventCategory.Event.EventDate,
        CategoryName = r.Enrolment.EventCategory.Category.CategoryName,
        PositionNo = r.PositionNo,
        FinishTime = r.FinishTime,
        PaceSecondsKm = r.PaceSecondsKm,
        ResultStatus = r.ResultStatus,
        RecordedOn = r.RecordedOn
    };
}
