using System.Text.Json;
using RaceDayNova.Api.Dtos;

namespace RaceDayNova.Tests;

/// <summary>Seed data matches the Part 1 SQL script; health, route, weather and Swagger endpoints.</summary>
public class SeedDataAndMiscTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SeedDataAndMiscTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeedData_MatchesThePart1Script()
    {
        var anonymous = _factory.CreateClient();
        var events = await anonymous.GetFromJsonAsync<List<EventDto>>("/api/events");
        var event2001 = await anonymous.GetFromJsonAsync<EventDetailDto>("/api/events/2001");

        Assert.Equal(3, events!.Count(e => e.EventId is 2001 or 2002 or 2003));
        Assert.Equal("Lowveld Sunrise 10K", event2001!.EventName);
        Assert.Equal(3, event2001.Categories.Count);

        var lebo = await TestHelpers.LoggedInAsync(_factory, TestHelpers.SeedParticipant1);
        Assert.Equal(2, (await lebo.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me"))!.Count);
        Assert.Equal(2, (await lebo.GetFromJsonAsync<List<ResultDto>>("/api/results/me"))!.Count);

        var thandi = await TestHelpers.LoggedInAsync(_factory, TestHelpers.SeedOrganiser1);
        var results = await thandi.GetFromJsonAsync<List<ResultDto>>("/api/events/2001/results");
        Assert.Equal(2, results!.Count);
    }

    [Fact]
    public async Task Health_ReportsHealthy()
    {
        var response = await _factory.CreateClient().GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var health = await response.Content.ReadFromJsonAsync<HealthDto>();
        Assert.Equal("Healthy", health!.Status);
    }

    [Fact]
    public async Task Route_ReturnsTheRouteReference_AndUnknownEventIs404()
    {
        var client = _factory.CreateClient();
        var route = await client.GetFromJsonAsync<RouteDto>("/api/events/2001/route");
        Assert.Equal("ROUTE-LV-10K", route!.RouteReference);
        Assert.Equal(10m, route.DistanceKm);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/events/999999/route")).StatusCode);
    }

    [Fact]
    public async Task Weather_ReturnsAFreshReading_AndIsStoredAsASnapshot()
    {
        var weather = await _factory.CreateClient().GetFromJsonAsync<WeatherDto>("/api/events/2001/weather");
        Assert.Equal("FakeProvider", weather!.ProviderName);
        Assert.False(weather.IsCached);

        // A second call within 3 hours is served from the stored snapshot.
        var again = await _factory.CreateClient().GetFromJsonAsync<WeatherDto>("/api/events/2001/weather");
        Assert.True(again!.IsCached);
    }

    [Fact]
    public async Task Weather_UnknownEvent_Returns404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _factory.CreateClient().GetAsync("/api/events/999999/weather")).StatusCode);
    }

    [Fact]
    public async Task Weather_WhenNoSnapshotExistsAndTheProviderIsDown_Returns503()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var request = TestHelpers.NewEventRequest();
        request.City = "Nowhere";
        var created = await (await organiser.PostAsJsonAsync("/api/events", request)).Content.ReadFromJsonAsync<EventDto>();

        var response = await _factory.CreateClient().GetAsync($"/api/events/{created!.EventId}/weather");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_ListsEveryEndpointFromThePart1Plan()
    {
        var json = await _factory.CreateClient().GetStringAsync("/swagger/v1/swagger.json");
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        var expected = new (string Path, string Method)[]
        {
            ("/api/auth/register", "post"), ("/api/auth/login", "post"), ("/api/auth/logout", "post"),
            ("/api/users/me", "get"), ("/api/users/me", "put"),
            ("/api/events", "get"), ("/api/events/{id}", "get"), ("/api/events", "post"),
            ("/api/events/{id}", "put"), ("/api/events/{id}", "delete"),
            ("/api/categories", "get"), ("/api/categories", "post"),
            ("/api/categories/{id}", "put"), ("/api/categories/{id}", "delete"),
            ("/api/events/{id}/categories", "get"), ("/api/events/{id}/categories", "post"),
            ("/api/events/{id}/categories/{categoryId}", "delete"),
            ("/api/events/{id}/enrolments", "post"), ("/api/enrolments/me", "get"),
            ("/api/events/{id}/enrolments", "get"), ("/api/enrolments/{id}", "delete"),
            ("/api/enrolments/{id}/result", "post"), ("/api/results/{id}", "put"),
            ("/api/results/me", "get"), ("/api/events/{id}/results", "get"),
            ("/api/events/{id}/route", "get"), ("/api/events/{id}/weather", "get"),
            ("/api/health", "get")
        };

        foreach (var (path, method) in expected)
        {
            Assert.True(paths.TryGetProperty(path, out var item), $"Swagger is missing path {path}");
            Assert.True(item.TryGetProperty(method, out _), $"Swagger is missing {method.ToUpperInvariant()} {path}");
        }
    }

    [Fact]
    public async Task Swagger_DescribesEachEndpoint_IncludingWhoMayCallIt()
    {
        var json = await _factory.CreateClient().GetStringAsync("/swagger/v1/swagger.json");
        using var doc = JsonDocument.Parse(json);
        var createEvent = doc.RootElement.GetProperty("paths").GetProperty("/api/events").GetProperty("post");

        Assert.Contains("Organiser", createEvent.GetProperty("description").GetString());
        Assert.True(createEvent.GetProperty("responses").TryGetProperty("403", out _));
    }

    [Fact]
    public async Task SwaggerUi_IsServed()
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
