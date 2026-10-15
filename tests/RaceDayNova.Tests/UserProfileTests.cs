using RaceDayNova.Api.Dtos;

namespace RaceDayNova.Tests;

/// <summary>Both roles can view and update only their own profile.</summary>
public class UserProfileTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public UserProfileTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task GetMe_ReturnsTheLoggedInUsersProfile_ForBothRoles(string role)
    {
        var (client, user) = await TestHelpers.NewUserAsync(_factory, role);

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(user.UserId, profile!.UserId);
        Assert.Equal(role, profile.UserRole);
    }

    [Fact]
    public async Task GetMe_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task UpdateMe_ChangesTheProfile_ButNeverTheRole()
    {
        var (client, user) = await TestHelpers.NewUserAsync(_factory, "Participant");

        var response = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = user.Email,
            Mobile = "+27829998888"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<UserDto>())!;
        Assert.Equal("Updated", updated.FirstName);
        Assert.Equal("+27829998888", updated.Mobile);
        Assert.Equal("Participant", updated.UserRole);
    }

    [Fact]
    public async Task UpdateMe_WithAnEmailUsedByAnotherAccount_Returns409()
    {
        var (client, user) = await TestHelpers.NewUserAsync(_factory, "Participant");

        var response = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = TestHelpers.SeedOrganiser1
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_WithInvalidData_Returns400()
    {
        var (client, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var response = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest { FirstName = "", LastName = "X", Email = "bad" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest { FirstName = "A", LastName = "B", Email = "a@b.co.za" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_ChangingPassword_RequiresTheCurrentPassword_AndTheNewOneWorks()
    {
        var (client, user) = await TestHelpers.NewUserAsync(_factory, "Participant");

        var wrong = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest
        {
            FirstName = user.FirstName, LastName = user.LastName, Email = user.Email,
            CurrentPassword = "NotMyPassword1", NewPassword = "BrandNewPass1"
        });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);

        var ok = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest
        {
            FirstName = user.FirstName, LastName = user.LastName, Email = user.Email,
            CurrentPassword = TestHelpers.Password, NewPassword = "BrandNewPass1"
        });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var fresh = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await fresh.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = user.Email, Password = "BrandNewPass1" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fresh.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = user.Email, Password = TestHelpers.Password })).StatusCode);
    }
}
