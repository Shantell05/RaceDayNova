using RaceDayNova.Api.Dtos;

namespace RaceDayNova.Tests;

/// <summary>Results: Organisers capture / correct them, Participants view only their own.</summary>
public class ResultTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ResultTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static ResultRequest Finished(int position, string time = "00:50:00") => new()
    {
        PositionNo = position,
        FinishTime = TimeSpan.Parse(time),
        ResultStatus = "Finished"
    };

    private async Task<(HttpClient Organiser, HttpClient Participant, EventDto Event, EnrolmentDto Enrolment)> EnrolledAsync()
    {
        var (organiser, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var (participant, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var enrolment = await TestHelpers.EnrolAsync(participant, ev.EventId, category.CategoryId);
        return (organiser, participant, ev, enrolment);
    }

    [Fact]
    public async Task Organiser_CanCaptureAResult_AndPaceIsCalculated()
    {
        var (organiser, _, ev, enrolment) = await EnrolledAsync(); // event distance is 10 km

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(3, "00:50:00"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<ResultDto>())!;
        Assert.Equal(3, result.PositionNo);
        Assert.Equal(new TimeSpan(0, 50, 0), result.FinishTime);
        Assert.Equal(300, result.PaceSecondsKm); // 3000 s / 10 km
        Assert.Equal("Finished", result.ResultStatus);
        Assert.Equal(ev.EventId, result.EventId);
    }

    [Fact]
    public async Task Participant_CannotCaptureAResult_Returns403()
    {
        var (_, participant, _, enrolment) = await EnrolledAsync();
        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(1));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCaptureAResult_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/enrolments/5001/result", Finished(1));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureAResultForAnotherOrganisersEvent_Returns403()
    {
        var (_, _, _, enrolment) = await EnrolledAsync();
        var (other, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var response = await other.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(1));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_UnknownEnrolment_Returns404()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/enrolments/999999/result", Finished(1));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _, _, enrolment) = await EnrolledAsync();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(1));

        var again = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(2));

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_FinishedWithoutFinishTime_Returns400()
    {
        var (organiser, _, _, enrolment) = await EnrolledAsync();
        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", new ResultRequest { PositionNo = 1, ResultStatus = "Finished" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithInvalidStatus_Returns400()
    {
        var (organiser, _, _, enrolment) = await EnrolledAsync();
        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", new ResultRequest { FinishTime = new TimeSpan(0, 40, 0), ResultStatus = "Winner" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_DidNotFinish_NeedsNoTimeOrPosition()
    {
        var (organiser, _, _, enrolment) = await EnrolledAsync();
        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", new ResultRequest { ResultStatus = "DNF" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("DNF", (await response.Content.ReadFromJsonAsync<ResultDto>())!.ResultStatus);
    }

    [Fact]
    public async Task CaptureResult_ForACancelledEnrolment_Returns400()
    {
        var (organiser, participant, _, enrolment) = await EnrolledAsync();
        await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithAPositionAlreadyTakenInTheCategory_Returns409()
    {
        var (organiser, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var (p1, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var (p2, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var e1 = await TestHelpers.EnrolAsync(p1, ev.EventId, category.CategoryId);
        var e2 = await TestHelpers.EnrolAsync(p2, ev.EventId, category.CategoryId);
        Assert.Equal(HttpStatusCode.Created, (await organiser.PostAsJsonAsync($"/api/enrolments/{e1.EnrolmentId}/result", Finished(1))).StatusCode);

        var clash = await organiser.PostAsJsonAsync($"/api/enrolments/{e2.EnrolmentId}/result", Finished(1));

        Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCorrectAResult()
    {
        var (organiser, _, _, enrolment) = await EnrolledAsync();
        var created = await (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(5, "00:55:00"))).Content.ReadFromJsonAsync<ResultDto>();

        var response = await organiser.PutAsJsonAsync($"/api/results/{created!.ResultId}", Finished(4, "00:45:00"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<ResultDto>())!;
        Assert.Equal(4, updated.PositionNo);
        Assert.Equal(new TimeSpan(0, 45, 0), updated.FinishTime);
        Assert.Equal(270, updated.PaceSecondsKm);
    }

    [Fact]
    public async Task CorrectResult_ByAnotherOrganiser_Returns403_AndUnknownIdReturns404()
    {
        var (organiser, _, _, enrolment) = await EnrolledAsync();
        var created = await (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(1))).Content.ReadFromJsonAsync<ResultDto>();
        var (other, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");

        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/results/{created!.ResultId}", Finished(2))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.PutAsJsonAsync("/api/results/999999", Finished(2))).StatusCode);
    }

    [Fact]
    public async Task Participant_SeesOnlyTheirOwnResults()
    {
        var (organiser, participant, ev, enrolment) = await EnrolledAsync();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(2));
        var (stranger, _) = await TestHelpers.NewUserAsync(_factory, "Participant");

        var mine = await participant.GetFromJsonAsync<List<ResultDto>>("/api/results/me");
        var strangers = await stranger.GetFromJsonAsync<List<ResultDto>>("/api/results/me");

        var result = Assert.Single(mine!);
        Assert.Equal(ev.EventId, result.EventId);
        Assert.Empty(strangers!);
    }

    [Fact]
    public async Task Organiser_CanViewEventResults_OrderedByPosition()
    {
        var (organiser, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var (p1, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var (p2, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var e1 = await TestHelpers.EnrolAsync(p1, ev.EventId, category.CategoryId);
        var e2 = await TestHelpers.EnrolAsync(p2, ev.EventId, category.CategoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{e1.EnrolmentId}/result", Finished(2, "00:55:00"));
        await organiser.PostAsJsonAsync($"/api/enrolments/{e2.EnrolmentId}/result", Finished(1, "00:45:00"));

        var results = await organiser.GetFromJsonAsync<List<ResultDto>>($"/api/events/{ev.EventId}/results");

        Assert.Equal(new int?[] { 1, 2 }, results!.Select(r => r.PositionNo).ToArray());
    }

    [Fact]
    public async Task EventResults_AreRestrictedToTheOwningOrganiser()
    {
        var (_, _, ev, _) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var (other, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var (participant, _) = await TestHelpers.NewUserAsync(_factory, "Participant");

        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/events/{ev.EventId}/results")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync($"/api/events/{ev.EventId}/results")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync("/api/events/999999/results")).StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_AfterAResultExists_Returns409()
    {
        var (organiser, participant, _, enrolment) = await EnrolledAsync();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", Finished(1));

        Assert.Equal(HttpStatusCode.Conflict, (await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}")).StatusCode);
    }
}
