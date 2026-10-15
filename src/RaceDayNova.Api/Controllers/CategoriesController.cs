using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayNova.Api.Data;
using RaceDayNova.Api.Dtos;
using RaceDayNova.Api.Models;
using RaceDayNova.Api.Security;

namespace RaceDayNova.Api.Controllers;

/// <summary>Reusable age / distance categories (e.g. Under 20, Senior, 10km, 21km).</summary>
[Route("api/categories")]
public class CategoriesController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>List all available categories.</summary>
    /// <response code="200">All categories, ordered by name.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(CancellationToken ct)
    {
        var categories = await _db.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync(ct);
        return Ok(categories.Select(c => c.ToDto()).ToList());
    }

    /// <summary>Create a reusable category.</summary>
    /// <remarks>Category names are unique. If both ages are given, AgeTo must be greater than or equal to AgeFrom.</remarks>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">A category with this name already exists.</response>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CategoryRequest request, CancellationToken ct)
    {
        var error = Validate(request);
        if (error is not null) return error;

        var name = request.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.CategoryName.ToLower() == name.ToLower(), ct))
            return Fail(StatusCodes.Status409Conflict, "Category already exists", $"A category named '{name}' already exists.");

        var category = new Category
        {
            CategoryName = name,
            AgeFrom = request.AgeFrom,
            AgeTo = request.AgeTo,
            CategoryNote = request.CategoryNote?.Trim()
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);
        return Created("/api/categories", category.ToDto());
    }

    /// <summary>Update a category.</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Another category already uses this name.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] CategoryRequest request, CancellationToken ct)
    {
        var error = Validate(request);
        if (error is not null) return error;

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id, ct);
        if (category is null) return Fail(StatusCodes.Status404NotFound, "Category not found");

        var name = request.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.CategoryId != id && c.CategoryName.ToLower() == name.ToLower(), ct))
            return Fail(StatusCodes.Status409Conflict, "Category already exists", $"A category named '{name}' already exists.");

        category.CategoryName = name;
        category.AgeFrom = request.AgeFrom;
        category.AgeTo = request.AgeTo;
        category.CategoryNote = request.CategoryNote?.Trim();
        await _db.SaveChangesAsync(ct);
        return Ok(category.ToDto());
    }

    /// <summary>Remove an unused category.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">The category is offered by at least one event and cannot be removed.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id, ct);
        if (category is null) return Fail(StatusCodes.Status404NotFound, "Category not found");

        if (await _db.EventCategories.AnyAsync(ec => ec.CategoryId == id, ct))
            return Fail(StatusCodes.Status409Conflict, "Category in use", "This category is offered by one or more events. Remove it from those events first.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private ObjectResult? Validate(CategoryRequest request)
    {
        if (request.AgeFrom.HasValue && request.AgeTo.HasValue && request.AgeTo < request.AgeFrom)
            return Fail(StatusCodes.Status400BadRequest, "Invalid age range", "AgeTo must be greater than or equal to AgeFrom.");
        return null;
    }
}
