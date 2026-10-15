using Microsoft.EntityFrameworkCore;
using RaceDayNova.Api.Models;

namespace RaceDayNova.Api.Data;

/// <summary>
/// Code-first EF Core model. Table names, column types, keys, indexes, defaults and CHECK constraints
/// mirror RaceDay_Nova_Database.sql and the Part 1 ERD (seven entities).
/// </summary>
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();
    public DbSet<EventWeatherSnapshot> EventWeatherSnapshots => Set<EventWeatherSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------- Users ----------
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_UserRole", "[UserRole] IN ('Organiser','Participant')"));
            e.HasKey(x => x.UserId).HasName("PK_Users");
            e.Property(x => x.UserId).UseIdentityColumn(1001, 1);
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(60);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(60);
            e.Property(x => x.Email).IsRequired().HasMaxLength(180);
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
            e.Property(x => x.Mobile).HasMaxLength(20);
            e.Property(x => x.UserRole).IsRequired().HasMaxLength(15).IsUnicode(false);
            e.Property(x => x.CreatedOn).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
        });

        // ---------- Events ----------
        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Running','Walking','Cycling')");
                t.HasCheckConstraint("CK_Events_Distance", "[DistanceKm] > 0");
                t.HasCheckConstraint("CK_Events_Status", "[EventStatus] IN ('Upcoming','Open','Closed','Completed','Cancelled')");
            });
            e.HasKey(x => x.EventId).HasName("PK_Events");
            e.Property(x => x.EventId).UseIdentityColumn(2001, 1);
            e.Property(x => x.EventName).IsRequired().HasMaxLength(160);
            e.Property(x => x.EventType).IsRequired().HasMaxLength(12).IsUnicode(false);
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.StartTime).HasColumnType("time(0)");
            e.Property(x => x.Venue).IsRequired().HasMaxLength(160);
            e.Property(x => x.City).IsRequired().HasMaxLength(80);
            e.Property(x => x.Province).IsRequired().HasMaxLength(80);
            e.Property(x => x.DistanceKm).HasPrecision(7, 2);
            e.Property(x => x.Description).HasMaxLength(800);
            e.Property(x => x.RouteReference).HasMaxLength(400);
            e.Property(x => x.EventStatus).IsRequired().HasMaxLength(12).IsUnicode(false).HasDefaultValue(EventStatuses.Upcoming);
            e.Property(x => x.CreatedOn).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents).HasForeignKey(x => x.OrganiserId)
                .HasConstraintName("FK_Events_Organiser").OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.OrganiserId).HasDatabaseName("IX_Events_Organiser");
            e.HasIndex(x => x.EventDate).HasDatabaseName("IX_Events_Date");
        });

        // ---------- Categories ----------
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories", t =>
            {
                t.HasCheckConstraint("CK_Categories_AgeFrom", "[AgeFrom] IS NULL OR [AgeFrom] >= 0");
                t.HasCheckConstraint("CK_Categories_AgeTo", "[AgeTo] IS NULL OR [AgeTo] >= 0");
                t.HasCheckConstraint("CK_Categories_AgeOrder", "[AgeTo] IS NULL OR [AgeFrom] IS NULL OR [AgeTo] >= [AgeFrom]");
            });
            e.HasKey(x => x.CategoryId).HasName("PK_Categories");
            e.Property(x => x.CategoryId).UseIdentityColumn(3001, 1);
            e.Property(x => x.CategoryName).IsRequired().HasMaxLength(100);
            e.Property(x => x.CategoryNote).HasMaxLength(250);
            e.HasIndex(x => x.CategoryName).IsUnique().HasDatabaseName("UQ_Categories_CategoryName");
        });

        // ---------- EventCategories (bridge) ----------
        modelBuilder.Entity<EventCategory>(e =>
        {
            e.ToTable("EventCategories", t =>
            {
                t.HasCheckConstraint("CK_EventCategories_Fee", "[EntryFee] >= 0");
                t.HasCheckConstraint("CK_EventCategories_Places", "[PlacesAvailable] IS NULL OR [PlacesAvailable] > 0");
            });
            e.HasKey(x => x.EventCategoryId).HasName("PK_EventCategories");
            e.Property(x => x.EventCategoryId).UseIdentityColumn(4001, 1);
            e.Property(x => x.EntryFee).HasPrecision(9, 2).HasDefaultValue(0m);
            e.HasOne(x => x.Event).WithMany(ev => ev.EventCategories).HasForeignKey(x => x.EventId)
                .HasConstraintName("FK_EventCategories_Event").OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany(c => c.EventCategories).HasForeignKey(x => x.CategoryId)
                .HasConstraintName("FK_EventCategories_Category").OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.EventId, x.CategoryId }).IsUnique().HasDatabaseName("UQ_EventCategories");
            e.HasIndex(x => x.EventId).HasDatabaseName("IX_EventCategories_Event");
        });

        // ---------- Enrolments ----------
        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[EnrolmentStatus] IN ('Pending','Confirmed','Cancelled')"));
            e.HasKey(x => x.EnrolmentId).HasName("PK_Enrolments");
            e.Property(x => x.EnrolmentId).UseIdentityColumn(5001, 1);
            e.Property(x => x.EnrolledOn).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.Property(x => x.EnrolmentStatus).IsRequired().HasMaxLength(12).IsUnicode(false).HasDefaultValue(EnrolmentStatuses.Confirmed);
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments).HasForeignKey(x => x.ParticipantId)
                .HasConstraintName("FK_Enrolments_Participant").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.EventCategory).WithMany(ec => ec.Enrolments).HasForeignKey(x => x.EventCategoryId)
                .HasConstraintName("FK_Enrolments_EventCategory").OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ParticipantId, x.EventCategoryId }).IsUnique().HasDatabaseName("UQ_Enrolments");
            e.HasIndex(x => x.ParticipantId).HasDatabaseName("IX_Enrolments_Participant");
            e.HasIndex(x => x.EventCategoryId).HasDatabaseName("IX_Enrolments_EventCategory");
        });

        // ---------- Results ----------
        modelBuilder.Entity<RaceResult>(e =>
        {
            e.ToTable("Results", t =>
            {
                t.HasCheckConstraint("CK_Results_Position", "[PositionNo] IS NULL OR [PositionNo] > 0");
                t.HasCheckConstraint("CK_Results_Pace", "[PaceSecondsKm] IS NULL OR [PaceSecondsKm] > 0");
                t.HasCheckConstraint("CK_Results_Status", "[ResultStatus] IN ('Finished','DNF','DNS','DSQ')");
            });
            e.HasKey(x => x.ResultId).HasName("PK_Results");
            e.Property(x => x.ResultId).UseIdentityColumn(6001, 1);
            e.Property(x => x.FinishTime).HasColumnType("time(0)");
            e.Property(x => x.ResultStatus).IsRequired().HasMaxLength(10).IsUnicode(false).HasDefaultValue(ResultStatuses.Finished);
            e.Property(x => x.RecordedOn).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result).HasForeignKey<RaceResult>(x => x.EnrolmentId)
                .HasConstraintName("FK_Results_Enrolment").OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_Enrolment");
        });

        // ---------- EventWeatherSnapshots ----------
        modelBuilder.Entity<EventWeatherSnapshot>(e =>
        {
            e.ToTable("EventWeatherSnapshots", t =>
            {
                t.HasCheckConstraint("CK_Weather_RainChance", "[RainChance] IS NULL OR [RainChance] BETWEEN 0 AND 100");
                t.HasCheckConstraint("CK_Weather_Wind", "[WindKph] IS NULL OR [WindKph] >= 0");
            });
            e.HasKey(x => x.WeatherSnapshotId).HasName("PK_EventWeatherSnapshots");
            e.Property(x => x.WeatherSnapshotId).UseIdentityColumn(7001, 1);
            e.Property(x => x.CheckedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.Property(x => x.TemperatureC).HasPrecision(5, 2);
            e.Property(x => x.WindKph).HasPrecision(6, 2);
            e.Property(x => x.RainChance).HasPrecision(5, 2);
            e.Property(x => x.Summary).HasMaxLength(160);
            e.Property(x => x.ProviderName).HasMaxLength(80);
            e.HasOne(x => x.Event).WithMany(ev => ev.WeatherSnapshots).HasForeignKey(x => x.EventId)
                .HasConstraintName("FK_Weather_Event").OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.EventId, x.CheckedAt }).HasDatabaseName("IX_Weather_EventDate");
        });

        SeedData.Apply(modelBuilder);
    }
}
