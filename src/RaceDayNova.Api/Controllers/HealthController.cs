using Microsoft.AspNetCore.Mvc;
using RaceDayNova.Api.Data;
using RaceDayNova.Api.Dtos;

namespace RaceDayNova.Api.Controllers;

[Route("api/health")]
public class HealthController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public HealthController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>API health check.</summary>
    /// <remarks>Confirms the API is running and that it can reach the SQL Server database.</remarks>
    /// <response code="200">API and database are healthy.</response>
    /// <response code="503">The API is running but cannot reach the database.</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var canConnect = await _db.Database.CanConnectAsync(ct);
        var dto = new HealthDto
        {
            Status = canConnect ? "Healthy" : "Unhealthy",
            Database = canConnect ? "Connected" : "Unavailable",
            TimestampUtc = DateTime.UtcNow
        };
        return canConnect ? Ok(dto) : StatusCode(StatusCodes.Status503ServiceUnavailable, dto);
    }
}
