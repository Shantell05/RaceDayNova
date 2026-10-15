 /*
 RaceDay Nova - Part 1 Database
 PROG6212 Programming 2B
 SQL Server / SSMS

 Seven-entity model:
 Users, Events, Categories, EventCategories,
 Enrolments, Results, EventWeatherSnapshots
*/

IF DB_ID('RaceDayNovaDB') IS NULL
    CREATE DATABASE RaceDayNovaDB;
GO

USE RaceDayNovaDB;
GO

/* Safe reset for repeatable testing. */
DROP TABLE IF EXISTS dbo.Results;
DROP TABLE IF EXISTS dbo.Enrolments;
DROP TABLE IF EXISTS dbo.EventCategories;
DROP TABLE IF EXISTS dbo.EventWeatherSnapshots;
DROP TABLE IF EXISTS dbo.Categories;
DROP TABLE IF EXISTS dbo.Events;
DROP TABLE IF EXISTS dbo.Users;
GO

CREATE TABLE dbo.Users
(
    UserId       INT IDENTITY(1001,1) NOT NULL,
    FirstName    NVARCHAR(60) NOT NULL,
    LastName     NVARCHAR(60) NOT NULL,
    Email        NVARCHAR(180) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    Mobile       NVARCHAR(20) NULL,
    UserRole     VARCHAR(15) NOT NULL CONSTRAINT CK_Users_UserRole CHECK (UserRole IN ('Organiser','Participant')),
    CreatedOn    DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedOn DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

CREATE TABLE dbo.Events
(
    EventId        INT IDENTITY(2001,1) NOT NULL,
    OrganiserId    INT NOT NULL,
    EventName      NVARCHAR(160) NOT NULL,
    EventType      VARCHAR(12) NOT NULL CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Running','Walking','Cycling')),
    EventDate      DATE NOT NULL,
    StartTime      TIME(0) NOT NULL,
    Venue          NVARCHAR(160) NOT NULL,
    City           NVARCHAR(80) NOT NULL,
    Province       NVARCHAR(80) NOT NULL,
    DistanceKm     DECIMAL(7,2) NOT NULL CONSTRAINT CK_Events_Distance CHECK (DistanceKm > 0),
    Description    NVARCHAR(800) NULL,
    RouteReference NVARCHAR(400) NULL,
    EventStatus   VARCHAR(12) NOT NULL CONSTRAINT DF_Events_Status DEFAULT 'Upcoming',
    CreatedOn      DATETIME2(0) NOT NULL CONSTRAINT DF_Events_CreatedOn DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Events_Status CHECK (EventStatus IN ('Upcoming','Open','Closed','Completed','Cancelled'))
);
GO

CREATE TABLE dbo.Categories
(
    CategoryId   INT IDENTITY(3001,1) NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    AgeFrom      SMALLINT NULL,
    AgeTo        SMALLINT NULL,
    CategoryNote NVARCHAR(250) NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_Categories_CategoryName UNIQUE (CategoryName),
    CONSTRAINT CK_Categories_AgeFrom CHECK (AgeFrom IS NULL OR AgeFrom >= 0),
    CONSTRAINT CK_Categories_AgeTo CHECK (AgeTo IS NULL OR AgeTo >= 0),
    CONSTRAINT CK_Categories_AgeOrder CHECK (AgeTo IS NULL OR AgeFrom IS NULL OR AgeTo >= AgeFrom)
);
GO

CREATE TABLE dbo.EventCategories
(
    EventCategoryId INT IDENTITY(4001,1) NOT NULL,
    EventId         INT NOT NULL,
    CategoryId      INT NOT NULL,
    EntryFee        DECIMAL(9,2) NOT NULL CONSTRAINT DF_EventCategories_Fee DEFAULT 0,
    PlacesAvailable INT NULL,
    CONSTRAINT PK_EventCategories PRIMARY KEY (EventCategoryId),
    CONSTRAINT UQ_EventCategories UNIQUE (EventId, CategoryId),
    CONSTRAINT FK_EventCategories_Event FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId) ON DELETE CASCADE,
    CONSTRAINT FK_EventCategories_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT CK_EventCategories_Fee CHECK (EntryFee >= 0),
    CONSTRAINT CK_EventCategories_Places CHECK (PlacesAvailable IS NULL OR PlacesAvailable > 0)
);
GO

CREATE TABLE dbo.Enrolments
(
    EnrolmentId     INT IDENTITY(5001,1) NOT NULL,
    ParticipantId   INT NOT NULL,
    EventCategoryId INT NOT NULL,
    EnrolledOn      DATETIME2(0) NOT NULL CONSTRAINT DF_Enrolments_EnrolledOn DEFAULT SYSDATETIME(),
    EnrolmentStatus VARCHAR(12) NOT NULL CONSTRAINT DF_Enrolments_Status DEFAULT 'Confirmed',
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT UQ_Enrolments UNIQUE (ParticipantId, EventCategoryId),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Enrolments_EventCategory FOREIGN KEY (EventCategoryId) REFERENCES dbo.EventCategories(EventCategoryId),
    CONSTRAINT CK_Enrolments_Status CHECK (EnrolmentStatus IN ('Pending','Confirmed','Cancelled'))
);
GO

CREATE TABLE dbo.Results
(
    ResultId       INT IDENTITY(6001,1) NOT NULL,
    EnrolmentId    INT NOT NULL,
    PositionNo     INT NULL,
    FinishTime     TIME(0) NULL,
    PaceSecondsKm  INT NULL,
    ResultStatus   VARCHAR(10) NOT NULL CONSTRAINT DF_Results_Status DEFAULT 'Finished',
    RecordedOn     DATETIME2(0) NOT NULL CONSTRAINT DF_Results_RecordedOn DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentId),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentId) REFERENCES dbo.Enrolments(EnrolmentId) ON DELETE CASCADE,
    CONSTRAINT CK_Results_Position CHECK (PositionNo IS NULL OR PositionNo > 0),
    CONSTRAINT CK_Results_Pace CHECK (PaceSecondsKm IS NULL OR PaceSecondsKm > 0),
    CONSTRAINT CK_Results_Status CHECK (ResultStatus IN ('Finished','DNF','DNS','DSQ'))
);
GO

CREATE TABLE dbo.EventWeatherSnapshots
(
    WeatherSnapshotId INT IDENTITY(7001,1) NOT NULL,
    EventId            INT NOT NULL,
    CheckedAt          DATETIME2(0) NOT NULL CONSTRAINT DF_Weather_CheckedAt DEFAULT SYSDATETIME(),
    TemperatureC       DECIMAL(5,2) NULL,
    WindKph            DECIMAL(6,2) NULL,
    RainChance         DECIMAL(5,2) NULL,
    Summary            NVARCHAR(160) NULL,
    ProviderName       NVARCHAR(80) NULL,
    CONSTRAINT PK_EventWeatherSnapshots PRIMARY KEY (WeatherSnapshotId),
    CONSTRAINT FK_Weather_Event FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId) ON DELETE CASCADE,
    CONSTRAINT CK_Weather_RainChance CHECK (RainChance IS NULL OR RainChance BETWEEN 0 AND 100),
    CONSTRAINT CK_Weather_Wind CHECK (WindKph IS NULL OR WindKph >= 0)
);
GO

CREATE INDEX IX_Events_Organiser ON dbo.Events(OrganiserId);
CREATE INDEX IX_Events_Date ON dbo.Events(EventDate);
CREATE INDEX IX_EventCategories_Event ON dbo.EventCategories(EventId);
CREATE INDEX IX_Enrolments_Participant ON dbo.Enrolments(ParticipantId);
CREATE INDEX IX_Enrolments_EventCategory ON dbo.Enrolments(EventCategoryId);
CREATE INDEX IX_Results_Enrolment ON dbo.Results(EnrolmentId);
CREATE INDEX IX_Weather_EventDate ON dbo.EventWeatherSnapshots(EventId, CheckedAt);
GO

/* Seed: minimum 2 organisers, 2 participants, 3 events and categories for every event. */
INSERT dbo.Users (FirstName, LastName, Email, PasswordHash, Mobile, UserRole)
VALUES
('Thandi','Maseko','thandi.maseko@racedaynova.co.za','PART1_HASH_001','+27821110001','Organiser'),
('Johan','Botha','johan.botha@racedaynova.co.za','PART1_HASH_002','+27821110002','Organiser'),
('Lebo','Molefe','lebo.molefe@racedaynova.co.za','PART1_HASH_003','+27821110003','Participant'),
('Zanele','Khumalo','zanele.khumalo@racedaynova.co.za','PART1_HASH_004','+27821110004','Participant');
GO

INSERT dbo.Events
(OrganiserId, EventName, EventType, EventDate, StartTime, Venue, City, Province, DistanceKm, Description, RouteReference, EventStatus)
VALUES
(1001,'Lowveld Sunrise 10K','Running','2026-10-10','06:30','Civic Sports Ground','Mbombela','Mpumalanga',10.00,'Road race through a scenic Lowveld route.','ROUTE-LV-10K','Open'),
(1002,'Cape Peninsula Cycle Day','Cycling','2026-11-08','06:00','Green Point Promenade','Cape Town','Western Cape',45.00,'Road cycling event for recreational and experienced riders.','ROUTE-CP-45K','Open'),
(1001,'Soweto Family Walk','Walking','2026-12-05','07:00','Orlando Community Stadium','Soweto','Gauteng',8.00,'Family-friendly community walking event.','ROUTE-SW-8K','Upcoming');
GO

INSERT dbo.Categories (CategoryName, AgeFrom, AgeTo, CategoryNote)
VALUES
('Open 18+ Run',18,NULL,'Open running category'),
('Junior Run',13,17,'Junior running category'),
('Veteran Run',40,NULL,'Veteran running category'),
('Open Cycle',18,NULL,'Open cycling category'),
('Junior Cycle',15,17,'Junior cycling category'),
('Family Walk',10,NULL,'Walking category for families and individuals');
GO

INSERT dbo.EventCategories (EventId, CategoryId, EntryFee, PlacesAvailable)
VALUES
(2001,3001,120.00,500),(2001,3002,80.00,150),(2001,3003,100.00,200),
(2002,3004,250.00,350),(2002,3005,150.00,100),
(2003,3006,60.00,600);
GO

INSERT dbo.Enrolments (ParticipantId, EventCategoryId, EnrolmentStatus)
VALUES
(1003,4001,'Confirmed'),
(1003,4004,'Confirmed'),
(1004,4002,'Confirmed'),
(1004,4006,'Confirmed');
GO

INSERT dbo.Results (EnrolmentId, PositionNo, FinishTime, PaceSecondsKm, ResultStatus)
VALUES
(5001,12,'00:52:30',315,'Finished'),
(5002,27,'02:05:00',280,'Finished'),
(5003,19,'00:48:20',290,'Finished');
GO

INSERT dbo.EventWeatherSnapshots (EventId, TemperatureC, WindKph, RainChance, Summary, ProviderName)
VALUES
(2001,19.50,12.00,10.00,'Cool morning with light wind','Part1 Sample Provider'),
(2002,17.20,18.50,25.00,'Breezy with a small chance of rain','Part1 Sample Provider'),
(2003,21.00,9.00,5.00,'Clear and mild','Part1 Sample Provider');
GO

/* Verification queries for SSMS evidence. */
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME;
SELECT UserRole, COUNT(*) AS TotalUsers FROM dbo.Users GROUP BY UserRole;
SELECT EventId, EventName, EventType, EventDate, EventStatus FROM dbo.Events ORDER BY EventDate;
SELECT ec.EventCategoryId, e.EventName, c.CategoryName, ec.EntryFee, ec.PlacesAvailable
FROM dbo.EventCategories ec
JOIN dbo.Events e ON e.EventId = ec.EventId
JOIN dbo.Categories c ON c.CategoryId = ec.CategoryId
ORDER BY e.EventId, ec.EventCategoryId;
SELECT * FROM dbo.Enrolments ORDER BY EnrolmentId;
SELECT * FROM dbo.Results ORDER BY ResultId;
SELECT * FROM dbo.EventWeatherSnapshots ORDER BY WeatherSnapshotId;
GO
