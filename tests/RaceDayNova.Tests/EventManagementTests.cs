using RaceDayNova.Api.Dtos;

namespace RaceDayNova.Tests;

/// <summary>Event management: Organisers create / update / delete events, everyone can browse them.</summary>
public class EventManagementTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EventManagementTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Organiser_CanCreateAnEvent_AndBecomesItsOwner()
    {
        var (organiser, user) = await TestHelpers.NewUserAsync(_factory, "Organiser");

        var ev = await TestHelpers.CreateEventAsync(organiser, type: "run");

        Assert.Equal(user.UserId, ev.OrganiserId);
        Assert.Equal("Running", ev.EventType); // alias "run" normalised
        Assert.Equal("Open", ev.EventStatus);
        Assert.True(ev.EventId > 0);
    }

    [Fact]
    public async Task CreateEvent_WithoutStatus_DefaultsToUpcoming()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var request = TestHelpers.NewEventRequest();
        request.EventStatus = null;

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Upcoming", (await response.Content.ReadFromJsonAsync<EventDto>())!.EventStatus);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Participant_CannotCreateAnEvent_Returns403()
    {
        var (participant, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var response = await participant.PostAsJsonAsync("/api/events", TestHelpers.NewEventRequest());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateAnEvent_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/events", TestHelpers.NewEventRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidType_Returns400()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var request = TestHelpers.NewEventRequest(type: "Swimming");
        Assert.Equal(HttpStatusCode.BadRequest, (await organiser.PostAsJsonAsync("/api/events", request)).StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidStatus_Returns400()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var request = TestHelpers.NewEventRequest(status: "Maybe");
        Assert.Equal(HttpStatusCode.BadRequest, (await organiser.PostAsJsonAsync("/api/events", request)).StatusCode);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    public async Task CreateEvent_WithNonPositiveDistance_Returns400(double distance)
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var request = TestHelpers.NewEventRequest();
        request.DistanceKm = (decimal)distance;
        Assert.Equal(HttpStatusCode.BadRequest, (await organiser.PostAsJsonAsync("/api/events", request)).StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithMissingRequiredFields_Returns400()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events", new { eventName = "Only a name" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_IsPublic_AndSupportsFilters()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var marker = TestHelpers.Unique("Filter");
        var running = await TestHelpers.CreateEventAsync(organiser, $"{marker} Run", type: "Running");
        await TestHelpers.CreateEventAsync(organiser, $"{marker} Ride", type: "Cycling");
        var anonymous = _factory.CreateClient();

        var all = await anonymous.GetFromJsonAsync<List<EventDto>>($"/api/events?search={marker}");
        var onlyRuns = await anonymous.GetFromJsonAsync<List<EventDto>>($"/api/events?search={marker}&eventType=run");

        Assert.Equal(2, all!.Count);
        Assert.Single(onlyRuns!);
        Assert.Equal(running.EventId, onlyRuns![0].EventId);
    }

    [Fact]
    public async Task GetEvents_WithAnInvalidFilter_Returns400()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events?eventType=swimming");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ReturnsTheEventWithItsCategories()
    {
        var (organiser, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);

        var detail = await _factory.CreateClient().GetFromJsonAsync<EventDetailDto>($"/api/events/{ev.EventId}");

        Assert.Equal(ev.EventName, detail!.EventName);
        var offered = Assert.Single(detail.Categories);
        Assert.Equal(category.CategoryId, offered.CategoryId);
        Assert.Equal(100m, offered.EntryFee);
    }

    [Fact]
    public async Task GetEvent_UnknownId_Returns404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _factory.CreateClient().GetAsync("/api/events/999999")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateTheirOwnEvent()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var update = TestHelpers.NewEventRequest("Renamed event", "Closed", "Cycling");
        update.DistanceKm = 42.2m;

        var response = await organiser.PutAsJsonAsync($"/api/events/{ev.EventId}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<EventDto>())!;
        Assert.Equal("Renamed event", updated.EventName);
        Assert.Equal("Cycling", updated.EventType);
        Assert.Equal("Closed", updated.EventStatus);
        Assert.Equal(42.2m, updated.DistanceKm);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var (other, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(owner);

        var response = await other.PutAsJsonAsync($"/api/events/{ev.EventId}", TestHelpers.NewEventRequest("Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_UnknownId_Returns404()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var response = await organiser.PutAsJsonAsync("/api/events/999999", TestHelpers.NewEventRequest());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteAnEventWithNoEnrolments()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.GetAsync($"/api/events/{ev.EventId}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var (other, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(owner);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/events/{ev.EventId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/events/{ev.EventId}")).StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_UnknownId_Returns404()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.DeleteAsync("/api/events/999999")).StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_CancelsItAndItsEnrolments_InsteadOfErasingHistory()
    {
        var (organiser, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var (participant, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        await TestHelpers.EnrolAsync(participant, ev.EventId, category.CategoryId);

        Assert.Equal(HttpStatusCode.NoContent, (await organiser.DeleteAsync($"/api/events/{ev.EventId}")).StatusCode);

        var detail = await _factory.CreateClient().GetFromJsonAsync<EventDetailDto>($"/api/events/{ev.EventId}");
        Assert.Equal("Cancelled", detail!.EventStatus);
        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");
        Assert.All(mine!.Where(m => m.EventId == ev.EventId), m => Assert.Equal("Cancelled", m.EnrolmentStatus));
    }
}
