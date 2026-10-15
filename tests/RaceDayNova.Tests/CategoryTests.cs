using RaceDayNova.Api.Dtos;

namespace RaceDayNova.Tests;

/// <summary>Reusable categories and the categories each event offers.</summary>
public class CategoryTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CategoryTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCategories_IsPublic_AndIncludesTheSeededCategories()
    {
        var categories = await _factory.CreateClient().GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        Assert.Contains(categories!, c => c.CategoryName == "Open 18+ Run");
        Assert.Contains(categories!, c => c.CategoryName == "Family Walk");
    }

    [Fact]
    public async Task Organiser_CanCreateACategory()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var name = TestHelpers.Unique("Under 20");

        var response = await organiser.PostAsJsonAsync("/api/categories", new CategoryRequest { CategoryName = name, AgeFrom = 0, AgeTo = 19 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
        Assert.Equal(name, created.CategoryName);
        Assert.Equal((short)19, created.AgeTo);
    }

    [Fact]
    public async Task Participant_CannotCreateACategory_Returns403()
    {
        var (participant, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        var response = await participant.PostAsJsonAsync("/api/categories", new CategoryRequest { CategoryName = TestHelpers.Unique("Nope") });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_Returns409()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var category = await TestHelpers.CreateCategoryAsync(organiser);

        var duplicate = await organiser.PostAsJsonAsync("/api/categories", new CategoryRequest { CategoryName = category.CategoryName.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithAgeToBelowAgeFrom_Returns400()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/categories", new CategoryRequest { CategoryName = TestHelpers.Unique("Bad"), AgeFrom = 40, AgeTo = 20 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateACategory_AndUnknownIdReturns404()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var category = await TestHelpers.CreateCategoryAsync(organiser);
        var newName = TestHelpers.Unique("Senior");

        var ok = await organiser.PutAsJsonAsync($"/api/categories/{category.CategoryId}", new CategoryRequest { CategoryName = newName, AgeFrom = 35 });
        var missing = await organiser.PutAsJsonAsync("/api/categories/999999", new CategoryRequest { CategoryName = newName });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(newName, (await ok.Content.ReadFromJsonAsync<CategoryDto>())!.CategoryName);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteAnUnusedCategory()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var category = await TestHelpers.CreateCategoryAsync(organiser);

        Assert.Equal(HttpStatusCode.NoContent, (await organiser.DeleteAsync($"/api/categories/{category.CategoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.DeleteAsync($"/api/categories/{category.CategoryId}")).StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_ThatAnEventUses_Returns409()
    {
        var (organiser, _, _, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        Assert.Equal(HttpStatusCode.Conflict, (await organiser.DeleteAsync($"/api/categories/{category.CategoryId}")).StatusCode);
    }

    // ---------- Event categories ----------

    [Fact]
    public async Task Anyone_CanListTheCategoriesOfAnEvent()
    {
        var (_, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory, places: 25);

        var list = await _factory.CreateClient().GetFromJsonAsync<List<EventCategoryDto>>($"/api/events/{ev.EventId}/categories");

        var item = Assert.Single(list!);
        Assert.Equal(category.CategoryId, item.CategoryId);
        Assert.Equal(25, item.PlacesAvailable);
        Assert.Equal(0, item.EnrolmentCount);
    }

    [Fact]
    public async Task ListEventCategories_UnknownEvent_Returns404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _factory.CreateClient().GetAsync("/api/events/999999/categories")).StatusCode);
    }

    [Fact]
    public async Task AddEventCategory_Twice_Returns409()
    {
        var (organiser, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var again = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new AddEventCategoryRequest { CategoryId = category.CategoryId, EntryFee = 50 });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task AddEventCategory_UnknownCategory_Returns400()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var response = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new AddEventCategoryRequest { CategoryId = 999999, EntryFee = 10 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddEventCategory_ToAnotherOrganisersEvent_Returns403()
    {
        var (_, _, ev, _) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var (other, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var category = await TestHelpers.CreateCategoryAsync(other);

        var response = await other.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new AddEventCategoryRequest { CategoryId = category.CategoryId, EntryFee = 10 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddEventCategory_ToUnknownEvent_Returns404()
    {
        var (organiser, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events/999999/categories", new AddEventCategoryRequest { CategoryId = 3001, EntryFee = 10 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanRemoveACategoryFromTheirEvent()
    {
        var (organiser, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);

        Assert.Equal(HttpStatusCode.NoContent, (await organiser.DeleteAsync($"/api/events/{ev.EventId}/categories/{category.CategoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.DeleteAsync($"/api/events/{ev.EventId}/categories/{category.CategoryId}")).StatusCode);
    }

    [Fact]
    public async Task RemoveEventCategory_FromAnotherOrganisersEvent_Returns403()
    {
        var (_, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var (other, _) = await TestHelpers.NewUserAsync(_factory, "Organiser");
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/events/{ev.EventId}/categories/{category.CategoryId}")).StatusCode);
    }

    [Fact]
    public async Task RemoveEventCategory_WithEnrolments_Returns409()
    {
        var (organiser, _, ev, category) = await TestHelpers.NewEventWithCategoryAsync(_factory);
        var (participant, _) = await TestHelpers.NewUserAsync(_factory, "Participant");
        await TestHelpers.EnrolAsync(participant, ev.EventId, category.CategoryId);

        Assert.Equal(HttpStatusCode.Conflict, (await organiser.DeleteAsync($"/api/events/{ev.EventId}/categories/{category.CategoryId}")).StatusCode);
    }
}
