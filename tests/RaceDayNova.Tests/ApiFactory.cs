using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDayNova.Api.Data;
using RaceDayNova.Api.Services;

namespace RaceDayNova.Tests;

/// <summary>
/// Starts the real API in memory for integration tests. SQL Server is swapped for the EF Core InMemory provider
/// (a fresh, isolated database per factory), and the live weather provider is swapped for a fake - so the tests
/// need no database server and no internet, and run identically on a laptop and in GitHub Actions.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string OrganiserInviteCode = "TEST-ORGANISER-CODE";

    private readonly string _databaseName = "RaceDayNovaTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Registration:OrganiserInviteCode"] = OrganiserInviteCode
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.RemoveAll<RaceDayDbContext>();
            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IWeatherProvider>();
            services.AddSingleton<IWeatherProvider, FakeWeatherProvider>();
        });
    }
}

/// <summary>Deterministic weather provider. A city called "Nowhere" simulates an unavailable provider.</summary>
public class FakeWeatherProvider : IWeatherProvider
{
    public string Name => "FakeProvider";

    public Task<WeatherReading?> GetCurrentAsync(string city, string province, CancellationToken ct)
    {
        if (string.Equals(city, "Nowhere", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<WeatherReading?>(null);

        return Task.FromResult<WeatherReading?>(new WeatherReading(22.5m, 11m, 15m, "Fake clear skies"));
    }
}
