namespace RaceDayNova.Tests;

/// <summary>
/// Role-based access across the whole API: Organiser functionality is closed to Participants (403) and vice versa,
/// and non-public endpoints reject anonymous callers (401). Authorisation runs before model binding, so an empty
/// body is enough to prove the guard.
/// </summary>
public class RoleEnforcementTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RoleEnforcementTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public static IEnumerable<object[]> OrganiserOnly => new[]
    {
        new object[] { "POST", "/api/events" },
        new object[] { "PUT", "/api/events/2001" },
        new object[] { "DELETE", "/api/events/2001" },
        new object[] { "POST", "/api/categories" },
        new object[] { "PUT", "/api/categories/3001" },
        new object[] { "DELETE", "/api/categories/3001" },
        new object[] { "POST", "/api/events/2001/categories" },
        new object[] { "DELETE", "/api/events/2001/categories/3001" },
        new object[] { "GET", "/api/events/2001/enrolments" },
        new object[] { "POST", "/api/enrolments/5001/result" },
        new object[] { "PUT", "/api/results/6001" },
        new object[] { "GET", "/api/events/2001/results" }
    };

    public static IEnumerable<object[]> ParticipantOnly => new[]
    {
        new object[] { "POST", "/api/events/2001/enrolments" },
        new object[] { "GET", "/api/enrolments/me" },
        new object[] { "DELETE", "/api/enrolments/5001" },
        new object[] { "GET", "/api/results/me" }
    };

    public static IEnumerable<object[]> AnyLoggedInUser => new[]
    {
        new object[] { "GET", "/api/users/me" },
        new object[] { "PUT", "/api/users/me" }
    };

    public static IEnumerable<object[]> Protected => OrganiserOnly.Concat(ParticipantOnly).Concat(AnyLoggedInUser);

    [Theory]
    [MemberData(nameof(OrganiserOnly))]
    public async Task OrganiserEndpoints_RejectParticipants_With403(string method, string url)
    {
        var participant = await TestHelpers.LoggedInAsync(_factory, TestHelpers.SeedParticipant1);
        var response = await TestHelpers.SendAsync(participant, method, url);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ParticipantOnly))]
    public async Task ParticipantEndpoints_RejectOrganisers_With403(string method, string url)
    {
        var organiser = await TestHelpers.LoggedInAsync(_factory, TestHelpers.SeedOrganiser1);
        var response = await TestHelpers.SendAsync(organiser, method, url);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Protected))]
    public async Task ProtectedEndpoints_RejectAnonymousCallers_With401(string method, string url)
    {
        var anonymous = _factory.CreateClient();
        var response = await TestHelpers.SendAsync(anonymous, method, url);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/health")]
    [InlineData("/api/events")]
    [InlineData("/api/events/2001")]
    [InlineData("/api/events/2001/categories")]
    [InlineData("/api/events/2001/route")]
    [InlineData("/api/events/2001/weather")]
    [InlineData("/api/categories")]
    public async Task PublicEndpoints_AreAvailableWithoutLogin(string url)
    {
        var response = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TheRoleIsTakenFromTheServerSession_NotFromAnythingTheClientSends()
    {
        var participant = await TestHelpers.LoggedInAsync(_factory, TestHelpers.SeedParticipant1);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/events") { Content = JsonContent.Create(new { }) };
        request.Headers.Add("X-Role", "Organiser");

        var response = await participant.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
