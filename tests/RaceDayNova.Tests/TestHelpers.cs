using RaceDayNova.Api.Dtos;

namespace RaceDayNova.Tests;

/// <summary>Shared helpers: create users, log them in, and create events / categories through the real API.</summary>
public static class TestHelpers
{
    public const string Password = "Password123!";

    public const string SeedOrganiser1 = "thandi.maseko@racedaynova.co.za"; // UserId 1001, owns events 2001 and 2003
    public const string SeedOrganiser2 = "johan.botha@racedaynova.co.za";   // UserId 1002, owns event 2002
    public const string SeedParticipant1 = "lebo.molefe@racedaynova.co.za"; // UserId 1003
    public const string SeedParticipant2 = "zanele.khumalo@racedaynova.co.za"; // UserId 1004

    public static string UniqueEmail(string prefix) => $"{prefix}.{Guid.NewGuid():N}@test.raceday.co.za";

    public static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}".Substring(0, Math.Min(prefix.Length + 9, 60));

    /// <summary>A brand-new client (own cookie jar), registered and logged in with the given role.</summary>
    public static async Task<(HttpClient Client, UserDto User)> NewUserAsync(ApiFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = UniqueEmail(role.ToLowerInvariant());

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Test",
            LastName = role,
            Email = email,
            Password = Password,
            Mobile = "+27821234567",
            Role = role,
            OrganiserInviteCode = role == "Organiser" ? ApiFactory.OrganiserInviteCode : null
        });
        register.EnsureSuccessStatusCode();

        var user = await LoginAsync(client, email, Password);
        return (client, user);
    }

    public static async Task<UserDto> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.User;
    }

    public static async Task<HttpClient> LoggedInAsync(ApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        await LoginAsync(client, email, Password);
        return client;
    }

    public static EventRequest NewEventRequest(string? name = null, string status = "Open", string type = "Running") => new()
    {
        EventName = name ?? Unique("Event"),
        EventType = type,
        EventDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
        StartTime = new TimeOnly(6, 30),
        Venue = "Test Venue",
        City = "Mbombela",
        Province = "Mpumalanga",
        DistanceKm = 10m,
        Description = "Created by an automated test",
        RouteReference = "ROUTE-TEST",
        EventStatus = status
    };

    public static async Task<EventDto> CreateEventAsync(HttpClient organiser, string? name = null, string status = "Open", string type = "Running")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEventRequest(name, status, type));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public static async Task<CategoryDto> CreateCategoryAsync(HttpClient organiser, string? name = null)
    {
        var response = await organiser.PostAsJsonAsync("/api/categories", new CategoryRequest
        {
            CategoryName = name ?? Unique("Cat"),
            AgeFrom = 18,
            CategoryNote = "Created by an automated test"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public static async Task<EventCategoryDto> AddCategoryToEventAsync(HttpClient organiser, int eventId, int categoryId, decimal fee = 100m, int? places = null)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new AddEventCategoryRequest { CategoryId = categoryId, EntryFee = fee, PlacesAvailable = places });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EventCategoryDto>())!;
    }

    /// <summary>An Organiser plus an open event that offers one fresh category.</summary>
    public static async Task<(HttpClient Organiser, UserDto OrganiserUser, EventDto Event, CategoryDto Category)> NewEventWithCategoryAsync(
        ApiFactory factory, int? places = null, string status = "Open")
    {
        var (organiser, user) = await NewUserAsync(factory, "Organiser");
        var ev = await CreateEventAsync(organiser, status: status);
        var category = await CreateCategoryAsync(organiser);
        await AddCategoryToEventAsync(organiser, ev.EventId, category.CategoryId, 100m, places);
        return (organiser, user, ev, category);
    }

    public static async Task<EnrolmentDto> EnrolAsync(HttpClient participant, int eventId, int categoryId)
    {
        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new EnrolRequest { CategoryId = categoryId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
    }

    public static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT") request.Content = JsonContent.Create(new { });
        return client.SendAsync(request);
    }
}
