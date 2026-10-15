using Microsoft.Extensions.DependencyInjection;
using RaceDayNova.Api.Data;
using RaceDayNova.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace RaceDayNova.Tests;

/// <summary>User registration and login (success and failure scenarios), session behaviour and password storage.</summary>
public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static RegisterRequest NewRegistration(string role = "Participant", string? email = null, string password = TestHelpers.Password, string? code = null) => new()
    {
        FirstName = "Sipho",
        LastName = "Dlamini",
        Email = email ?? TestHelpers.UniqueEmail("reg"),
        Password = password,
        Mobile = "+27831112222",
        Role = role,
        OrganiserInviteCode = code
    };

    // ---------- Registration ----------

    [Fact]
    public async Task Register_Participant_Returns201AndTheProfile_WithoutExposingThePassword()
    {
        var client = _factory.CreateClient();
        var request = NewRegistration();

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);
        var user = System.Text.Json.JsonSerializer.Deserialize<UserDto>(raw, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Equal("Participant", user!.UserRole);
        Assert.Equal(request.Email, user.Email);
    }

    [Fact]
    public async Task Register_StoresAHashedPassword_NeverThePlainText()
    {
        var client = _factory.CreateClient();
        var request = NewRegistration();
        await client.PostAsJsonAsync("/api/auth/register", request);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var stored = await db.Users.AsNoTracking().SingleAsync(u => u.Email == request.Email);

        Assert.NotEqual(request.Password, stored.PasswordHash);
        Assert.StartsWith("PBKDF2$", stored.PasswordHash);
    }

    [Fact]
    public async Task Register_OrganiserWithValidInviteCode_Returns201()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegistration("Organiser", code: ApiFactory.OrganiserInviteCode));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithoutInviteCode_Returns403()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegistration("Organiser"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithWrongInviteCode_Returns403()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegistration("Organiser", code: "WRONG-CODE"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var request = NewRegistration();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", request)).StatusCode);

        var duplicate = await client.PostAsJsonAsync("/api/auth/register", NewRegistration(email: request.Email.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email", "Password123!", "Participant")]   // invalid email
    [InlineData("valid@test.raceday.co.za", "short1", "Participant")] // too short
    [InlineData("valid@test.raceday.co.za", "nodigitshere", "Participant")] // no digit
    [InlineData("valid@test.raceday.co.za", "Password123!", "Admin")] // unknown role
    public async Task Register_InvalidInput_Returns400(string email, string password, string role)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegistration(role, email, password));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingRequiredFields_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "x@y.com" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Login / session ----------

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200AndCreatesASession()
    {
        var client = _factory.CreateClient();
        var request = NewRegistration();
        await client.PostAsJsonAsync("/api/auth/register", request);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = request.Email, Password = request.Password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.True(login.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.StartsWith(".RaceDayNova.Session"));

        // The session now authenticates later requests without sending credentials again.
        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(request.Email, (await me.Content.ReadFromJsonAsync<UserDto>())!.Email);
    }

    [Fact]
    public async Task Login_EmailIsCaseInsensitive()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = TestHelpers.SeedParticipant1.ToUpperInvariant(), Password = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401_AndNoSession()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = TestHelpers.SeedParticipant1, Password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = "nobody@test.raceday.co.za", Password = "Password123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Theory]
    [InlineData(TestHelpers.SeedOrganiser1, "Organiser")]
    [InlineData(TestHelpers.SeedOrganiser2, "Organiser")]
    [InlineData(TestHelpers.SeedParticipant1, "Participant")]
    [InlineData(TestHelpers.SeedParticipant2, "Participant")]
    public async Task Login_SeededUsers_CanLogInWithTheDocumentedPassword_AndGetTheirRole(string email, string expectedRole)
    {
        var client = _factory.CreateClient();
        var user = await TestHelpers.LoginAsync(client, email, TestHelpers.Password);
        Assert.Equal(expectedRole, user.UserRole);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var (client, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task Sessions_AreIsolatedPerClient()
    {
        var (clientA, userA) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var (clientB, userB) = await TestHelpers.NewUserAsync(_factory, "Organiser");

        Assert.Equal(userA.UserId, (await (await clientA.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<UserDto>())!.UserId);
        Assert.Equal(userB.UserId, (await (await clientB.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<UserDto>())!.UserId);
    }
}
