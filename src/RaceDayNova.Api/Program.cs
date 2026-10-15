using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDayNova.Api.Data;
using RaceDayNova.Api.Security;
using RaceDayNova.Api.Services;
using RaceDayNova.Api.Swagger;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database (SQL Server via EF Core, code-first) ----------
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayNovaDb")));

// ---------- Security: password hashing + server-side session ----------
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDayNova.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(builder.Configuration.GetValue("Session:IdleTimeoutMinutes", 60));
});

// ---------- Weather provider ----------
builder.Services.AddHttpClient<IWeatherProvider, OpenMeteoWeatherProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});

// ---------- MVC + Swagger ----------
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay Nova API",
        Version = "v1",
        Description =
            "REST API for RaceDay Nova - event management for the South African running, walking and cycling community.\n\n" +
            "**How to use this page**\n" +
            "1. Register with `POST /api/auth/register` (Participants register freely; Organisers need the organiser invite code).\n" +
            "2. Log in with `POST /api/auth/login`. The server creates a session and the browser stores the session cookie automatically.\n" +
            "3. Try any endpoint. Each endpoint lists who may call it. 401 = not logged in, 403 = wrong role or not the owner.\n\n" +
            "Seeded demo users (password `Password123!`): Organisers thandi.maseko@racedaynova.co.za and johan.botha@racedaynova.co.za; " +
            "Participants lebo.molefe@racedaynova.co.za and zanele.khumalo@racedaynova.co.za."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    options.OperationFilter<SessionAccessOperationFilter>();
});

var app = builder.Build();

// ---------- Create / migrate the database ----------
if (app.Configuration.GetValue("Database:AutoInitialise", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    if (db.Database.IsRelational() && db.Database.GetMigrations().Any())
        db.Database.Migrate();      // normal path once 'dotnet ef migrations add InitialCreate' has been run
    else
        db.Database.EnsureCreated(); // fallback: builds the schema straight from the EF model
}

// ---------- HTTP pipeline ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}
app.UseStatusCodePages();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay Nova API v1");
    options.DocumentTitle = "RaceDay Nova API";
    options.DisplayRequestDuration();
});

app.UseSession();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

app.Run();

/// <summary>Exposed so the integration tests can start the API with WebApplicationFactory.</summary>
public partial class Program { }
