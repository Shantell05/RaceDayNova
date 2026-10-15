using RaceDayNova.Api.Dtos;

namespace RaceDayNova.Tests;

/// <summary>Event enrolments: Participants enter a category and the link is recorded; Organisers see all enrolments.</summary>
public class EnrolmentTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EnrolmentTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_CanEnrol_AndTheEnrolmentIsCorrectlyRecorded()
    {
        var (organiser, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var (participant, user) = await TestHelpers.NewUserAsync(_factory, "Participant");

        var enrolment = await TestHelpers.EnrolAsync(participant, ev.EventId, category.CategoryId);

        // The record links participant + event + category.
        Assert.Equal(user.UserId, enrolment.ParticipantId);
        Assert.Equal(ev.EventId, enrolment.EventId);
        Assert.Equal(category.CategoryId, enrolment.CategoryId);
        Assert.Equal("Confirmed", enrolment.EnrolmentStatus);

        // The participant sees it...
        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");
        Assert.Contains(mine!, e => e.EnrolmentId == enrolment.EnrolmentId);

        // ...and the organiser sees it for their event.
        var forEvent = await organiser.GetFromJsonAsync<List<EnrolmentDto>>($"/api/events/{ev.EventId}/enrolments");
        var seen = Assert.Single(forEvent!);
        Assert.Equal(user.UserId, seen.ParticipantId);

        // The category's enrolment count reflects it.
        var categories = await _factory.CreateClient().GetFromJsonAsync<List<EventCategoryDto>>($"/api/events/{ev.EventId}/categories");
        Assert.Equal(1, categories![0].EnrolmentCount);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var (organiser, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var response = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", new EnrolRequest { CategoryId = category.CategoryId });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotEnrol_Returns401()
    {
        var (_, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var response = await _factory.CreateClient().PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", new EnrolRequest { CategoryId = category.CategoryId });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_TwiceInTheSameEvent_Returns409()
    {
        var (_, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var (participant, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        await TestHelpers.EnrolAsync(participant, ev.EventId, category.CategoryId);

        var again = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", new EnrolRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Enrol_InACategoryTheEventDoesNotOffer_Returns400()
    {
        var (organiser, _, ev, _) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var unrelated = await TestHelpers.CreateCategoryAsync(organiser);
        var (participant, _) = await TestHelpers.NewUserAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", new EnrolRequest { CategoryId = unrelated.CategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InAnUnknownEvent_Returns404()
    {
        var (participant, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var response = await participant.PostAsJsonAsync("/api/events/999999/enrolments", new EnrolRequest { CategoryId = 3001 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Closed")]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    public async Task Enrol_InAnEventThatIsNotOpen_Returns400(string status)
    {
        var (_, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory, status: status);
        var (participant, _) = await TestHelpers.NewUserAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", new EnrolRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenTheCategoryIsFull_Returns409()
    {
        var (_, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory, places: 1);
        var (first, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var (second, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        await TestHelpers.EnrolAsync(first, ev.EventId, category.CategoryId);

        var response = await second.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", new EnrolRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelTheirEnrolment_FreeingThePlace_AndCanReEnrolAfterwards()
    {
        var (_, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory, places: 1);
        var (participant, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var enrolment = await TestHelpers.EnrolAsync(participant, ev.EventId, category.CategoryId);

        Assert.Equal(HttpStatusCode.NoContent, (await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}")).StatusCode);
        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");
        Assert.Equal("Cancelled", mine!.Single(e => e.EnrolmentId == enrolment.EnrolmentId).EnrolmentStatus);

        var again = await TestHelpers.EnrolAsync(participant, ev.EventId, category.CategoryId);
        Assert.Equal(enrolment.EnrolmentId, again.EnrolmentId); // re-activated, not duplicated
        Assert.Equal("Confirmed", again.EnrolmentStatus);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment_Returns403()
    {
        var (_, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var (owner, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var (other, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var enrolment = await TestHelpers.EnrolAsync(owner, ev.EventId, category.CategoryId);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}")).StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_UnknownId_Returns404()
    {
        var (participant, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        Assert.Equal(HttpStatusCode.NotFound, (await participant.DeleteAsync("/api/enrolments/999999")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsOfAnotherOrganisersEvent_Returns403()
    {
        var (_, _, ev, _) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var (other, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/events/{ev.EventId}/enrolments")).StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewAnEventsEnrolments_Returns403()
    {
        var (_, _, ev, _) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var (participant, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync($"/api/events/{ev.EventId}/enrolments")).StatusCode);
    }

    [Fact]
    public async Task ViewEventEnrolments_UnknownEvent_Returns404()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.GetAsync("/api/events/999999/enrolments")).StatusCode);
    }
}
