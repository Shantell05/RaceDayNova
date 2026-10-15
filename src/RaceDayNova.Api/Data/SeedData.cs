using Microsoft.EntityFrameworkCore;
using RaceDayNova.Api.Models;

namespace RaceDayNova.Api.Data;

/// <summary>
/// Seed data identical to the Part 1 SQL script (2 organisers, 2 participants, 3 events, categories for every event,
/// enrolments, results and weather snapshots) - except that the placeholder password hashes are replaced with real
/// PBKDF2 hashes so the seeded users can log in. Every seeded user's password is: Password123!
/// </summary>
public static class SeedData
{
    public const string DemoPassword = "Password123!";

    // PBKDF2$iterations$salt$hash - generated with the same algorithm as Pbkdf2PasswordHasher.
    private const string HashThandi = "PBKDF2$210000$obLD1OX2BxgpOktcbX6PkA==$CQ48tssx3q13qtRSNTBOWdqkGek70mIQ49XpWpQUUAY=";
    private const string HashJohan = "PBKDF2$210000$Dx4tPEtaaXiHlqW0w9Lh8A==$MTlLlKeh57PoNRvk96R4DWpUKI9HkzRGz7Q6OSUEwwY=";
    private const string HashLebo = "PBKDF2$210000$Wmt8jZ4PGis8TV5vcIGSow==$bvuwQQSdoZEuv725nN1zHTF33tfvYQ75gF6Uj82ucvA=";
    private const string HashZanele = "PBKDF2$210000$kYJzZFVGNygZAKq7zN3u/w==$ijN54yb+uoqyhuxTlItcQPVIf4scq2j9vhJFIF8nkYQ=";

    public static string SeededHashFor(string email) => email switch
    {
        "thandi.maseko@racedaynova.co.za" => HashThandi,
        "johan.botha@racedaynova.co.za" => HashJohan,
        "lebo.molefe@racedaynova.co.za" => HashLebo,
        "zanele.khumalo@racedaynova.co.za" => HashZanele,
        _ => string.Empty
    };

    public static void Apply(ModelBuilder modelBuilder)
    {
        var created = new DateTime(2026, 9, 1, 8, 0, 0);

        modelBuilder.Entity<AppUser>().HasData(
            new AppUser { UserId = 1001, FirstName = "Thandi", LastName = "Maseko", Email = "thandi.maseko@racedaynova.co.za", PasswordHash = HashThandi, Mobile = "+27821110001", UserRole = Roles.Organiser, CreatedOn = created },
            new AppUser { UserId = 1002, FirstName = "Johan", LastName = "Botha", Email = "johan.botha@racedaynova.co.za", PasswordHash = HashJohan, Mobile = "+27821110002", UserRole = Roles.Organiser, CreatedOn = created },
            new AppUser { UserId = 1003, FirstName = "Lebo", LastName = "Molefe", Email = "lebo.molefe@racedaynova.co.za", PasswordHash = HashLebo, Mobile = "+27821110003", UserRole = Roles.Participant, CreatedOn = created },
            new AppUser { UserId = 1004, FirstName = "Zanele", LastName = "Khumalo", Email = "zanele.khumalo@racedaynova.co.za", PasswordHash = HashZanele, Mobile = "+27821110004", UserRole = Roles.Participant, CreatedOn = created });

        modelBuilder.Entity<Event>().HasData(
            new Event { EventId = 2001, OrganiserId = 1001, EventName = "Lowveld Sunrise 10K", EventType = EventTypes.Running, EventDate = new DateOnly(2026, 10, 10), StartTime = new TimeOnly(6, 30), Venue = "Civic Sports Ground", City = "Mbombela", Province = "Mpumalanga", DistanceKm = 10.00m, Description = "Road race through a scenic Lowveld route.", RouteReference = "ROUTE-LV-10K", EventStatus = EventStatuses.Open, CreatedOn = created },
            new Event { EventId = 2002, OrganiserId = 1002, EventName = "Cape Peninsula Cycle Day", EventType = EventTypes.Cycling, EventDate = new DateOnly(2026, 11, 8), StartTime = new TimeOnly(6, 0), Venue = "Green Point Promenade", City = "Cape Town", Province = "Western Cape", DistanceKm = 45.00m, Description = "Road cycling event for recreational and experienced riders.", RouteReference = "ROUTE-CP-45K", EventStatus = EventStatuses.Open, CreatedOn = created },
            new Event { EventId = 2003, OrganiserId = 1001, EventName = "Soweto Family Walk", EventType = EventTypes.Walking, EventDate = new DateOnly(2026, 12, 5), StartTime = new TimeOnly(7, 0), Venue = "Orlando Community Stadium", City = "Soweto", Province = "Gauteng", DistanceKm = 8.00m, Description = "Family-friendly community walking event.", RouteReference = "ROUTE-SW-8K", EventStatus = EventStatuses.Upcoming, CreatedOn = created });

        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryId = 3001, CategoryName = "Open 18+ Run", AgeFrom = 18, AgeTo = null, CategoryNote = "Open running category" },
            new Category { CategoryId = 3002, CategoryName = "Junior Run", AgeFrom = 13, AgeTo = 17, CategoryNote = "Junior running category" },
            new Category { CategoryId = 3003, CategoryName = "Veteran Run", AgeFrom = 40, AgeTo = null, CategoryNote = "Veteran running category" },
            new Category { CategoryId = 3004, CategoryName = "Open Cycle", AgeFrom = 18, AgeTo = null, CategoryNote = "Open cycling category" },
            new Category { CategoryId = 3005, CategoryName = "Junior Cycle", AgeFrom = 15, AgeTo = 17, CategoryNote = "Junior cycling category" },
            new Category { CategoryId = 3006, CategoryName = "Family Walk", AgeFrom = 10, AgeTo = null, CategoryNote = "Walking category for families and individuals" });

        modelBuilder.Entity<EventCategory>().HasData(
            new EventCategory { EventCategoryId = 4001, EventId = 2001, CategoryId = 3001, EntryFee = 120.00m, PlacesAvailable = 500 },
            new EventCategory { EventCategoryId = 4002, EventId = 2001, CategoryId = 3002, EntryFee = 80.00m, PlacesAvailable = 150 },
            new EventCategory { EventCategoryId = 4003, EventId = 2001, CategoryId = 3003, EntryFee = 100.00m, PlacesAvailable = 200 },
            new EventCategory { EventCategoryId = 4004, EventId = 2002, CategoryId = 3004, EntryFee = 250.00m, PlacesAvailable = 350 },
            new EventCategory { EventCategoryId = 4005, EventId = 2002, CategoryId = 3005, EntryFee = 150.00m, PlacesAvailable = 100 },
            new EventCategory { EventCategoryId = 4006, EventId = 2003, CategoryId = 3006, EntryFee = 60.00m, PlacesAvailable = 600 });

        modelBuilder.Entity<Enrolment>().HasData(
            new Enrolment { EnrolmentId = 5001, ParticipantId = 1003, EventCategoryId = 4001, EnrolledOn = created, EnrolmentStatus = EnrolmentStatuses.Confirmed },
            new Enrolment { EnrolmentId = 5002, ParticipantId = 1003, EventCategoryId = 4004, EnrolledOn = created, EnrolmentStatus = EnrolmentStatuses.Confirmed },
            new Enrolment { EnrolmentId = 5003, ParticipantId = 1004, EventCategoryId = 4002, EnrolledOn = created, EnrolmentStatus = EnrolmentStatuses.Confirmed },
            new Enrolment { EnrolmentId = 5004, ParticipantId = 1004, EventCategoryId = 4006, EnrolledOn = created, EnrolmentStatus = EnrolmentStatuses.Confirmed });

        modelBuilder.Entity<RaceResult>().HasData(
            new RaceResult { ResultId = 6001, EnrolmentId = 5001, PositionNo = 12, FinishTime = new TimeSpan(0, 52, 30), PaceSecondsKm = 315, ResultStatus = ResultStatuses.Finished, RecordedOn = created },
            new RaceResult { ResultId = 6002, EnrolmentId = 5002, PositionNo = 27, FinishTime = new TimeSpan(2, 5, 0), PaceSecondsKm = 280, ResultStatus = ResultStatuses.Finished, RecordedOn = created },
            new RaceResult { ResultId = 6003, EnrolmentId = 5003, PositionNo = 19, FinishTime = new TimeSpan(0, 48, 20), PaceSecondsKm = 290, ResultStatus = ResultStatuses.Finished, RecordedOn = created });

        modelBuilder.Entity<EventWeatherSnapshot>().HasData(
            new EventWeatherSnapshot { WeatherSnapshotId = 7001, EventId = 2001, CheckedAt = created, TemperatureC = 19.50m, WindKph = 12.00m, RainChance = 10.00m, Summary = "Cool morning with light wind", ProviderName = "Part1 Sample Provider" },
            new EventWeatherSnapshot { WeatherSnapshotId = 7002, EventId = 2002, CheckedAt = created, TemperatureC = 17.20m, WindKph = 18.50m, RainChance = 25.00m, Summary = "Breezy with a small chance of rain", ProviderName = "Part1 Sample Provider" },
            new EventWeatherSnapshot { WeatherSnapshotId = 7003, EventId = 2003, CheckedAt = created, TemperatureC = 21.00m, WindKph = 9.00m, RainChance = 5.00m, Summary = "Clear and mild", ProviderName = "Part1 Sample Provider" });
    }
}
