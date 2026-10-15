# RaceDay Nova – Part 1

**Module:** PROG6212 Programming 2B  
**Submission:** Part 1 – System Planning and Database

RaceDay Nova is a proposed full-stack event-management platform for the South African running, walking and cycling community. The system separates Organiser and Participant permissions and provides the database/API foundation required for Parts 2 and 3.

## User roles

### Organiser
- Create, edit and delete/cancel events.
- Manage event categories.
- View event enrolments.
- Capture and update participant results.

### Participant
- Register and log in.
- Browse events and categories.
- Enrol in an event category.
- View own enrolments.
- View personal performance history.

## Part 1 evidence

| File | Purpose |
|---|---|
| `docs/RaceDay_Nova_ERD.png` | Seven-entity ERD with PKs, FKs and cardinalities |
| `docs/RaceDay_Nova_API_Endpoint_Plan.md` | Structured endpoint plan |
| `docs/RaceDay_Nova_Database.sql` | SQL Server schema and realistic seed data |
| `docs/Part1_Video_Script.md` | Script for student's own-voice video |
| `docs/20_Meaningful_Commits_Plan.md` | Suggested meaningful commit sequence |
| `docs/Part1_Rubric_Traceability.md` | Rubric-to-evidence mapping |
| `.github/workflows/part1-validation.yml` | CI validation workflow |

## ERD design

The model contains seven entities: Users, Events, Categories, EventCategories, Enrolments, Results and EventWeatherSnapshots.

- One organiser can manage many events.
- Events and Categories are many-to-many through EventCategories.
- One participant can have many enrolments.
- One event category can receive many enrolments.
- An enrolment can have zero or one result.
- An event can have many weather snapshots.

  <img width="1572" height="747" alt="RaceDay_Nova_ERD" src="https://github.com/user-attachments/assets/2a5cf10e-c5c8-4c8d-88ed-59067755ee43" />


## SQL Server / SSMS

1. Open `docs/RaceDay_Nova_Database.sql` in SQL Server Management Studio.
2. Execute the script on a SQL Server instance.
3. Confirm `RaceDayNovaDB` is created.
4. Confirm all seven tables exist.
5. Run the verification queries at the end of the script.
6. Capture your own SSMS evidence for the presentation.

The script contains primary keys, foreign keys, `NOT NULL`, `UNIQUE`, `DEFAULT` and `CHECK` constraints. Seed data includes two organisers, two participants, three events, categories for each event, sample enrolments, results and weather records.

## GitHub and CI/CD

The workflow validates that the required Part 1 evidence exists and performs basic content checks on the SQL and API plan. Push the repository to your own GitHub account and show the resulting green workflow run in your README/evidence.
<img width="1311" height="639" alt="BLANK" src="https://github.com/user-attachments/assets/aacc56fb-b3a3-476c-962a-6a9b1447095e" />
<img width="1355" height="623" alt="VALID" src="https://github.com/user-attachments/assets/bf9b3bf8-33a8-4a74-b868-fecd983db1c4" />
<img width="1335" height="567" alt="CI" src="https://github.com/user-attachments/assets/d34e98a6-51c7-497e-9fd2-e0e4f22cdb24" />




 

## Video YOUTUBE LINK

**Part 1 video:** https://youtu.be/KsMqBe7p3sM?si=PWfNzfC0imJQRkJW

## GitHub LINK

https://github.com/Shantell05/RaceDay_Nova_COMPLETE_Part1_Planning/tree/master/RaceDay_Nova_Part1


## Final submission checklist

-  Repository pushed to your own GitHub account.
-  `/docs` contains the ERD PNG.
-  `/docs` contains the endpoint plan.
-  `/docs` contains the SQL script.
-  SQL script tested successfully in SSMS.
-  Minimum 20 meaningful commits made using your own account.
-  GitHub Actions workflow is green.
-  Screenshot of the real green build added to evidence/README.
-  Own-voice Part 1 video recorded.
-  Video uploaded as unlisted to YouTube.
-  Real YouTube link added to README.
- GitHub repository link submitted on ARC.


## Word Planning Document

Detailed Part 1 planning document: `docs/RaceDay_Nova_Part1_Planning.docx`.




# RaceDay Nova - Part 2: RESTful API

PROG6212 Programming 2B - Part 2 (Restful API Development)
Student: **Shantell Malatjie (ST10475153)**

RaceDay Nova is an event management system for the South African running, walking and cycling community.
This repository holds the **ASP.NET Core 8 Web API** that powers it: SQL Server via Entity Framework Core (code-first),
server-side session authentication, role-based access for **Organisers** and **Participants**, Swagger, unit/integration
tests and a GitHub Actions CI/CD pipeline. The design follows the Part 1 ERD, SQL script and endpoint plan (see `/docs`).

| | |
|---|---|
| **GitHub repository** | `PASTE YOUR REPOSITORY LINK HERE` |
| **YouTube video (unlisted)** | `PASTE YOUR UNLISTED YOUTUBE LINK HERE` |

---

## Repository structure

```
RaceDayNova.sln
.github/workflows/part2-ci.yml     GitHub Actions: validate structure, build, run all tests
docs/                              Part 1 ERD, SQL script, endpoint plan + Part 2 notes
src/RaceDayNova.Api/
  Controllers/                     Auth, Users, Events, EventCategories, Categories, Enrolments, Results, Health
  Models/                          Seven EF Core entities + constants (roles, statuses)
  Data/                            RaceDayDbContext (Fluent API) and SeedData
  Dtos/                            Request / response models and mappers
  Security/                        PBKDF2 password hasher, [SessionAuthorize] attribute
  Services/                        Weather provider (Open-Meteo)
  Swagger/                         Operation filter that documents access rules
tests/RaceDayNova.Tests/           xUnit tests (integration + unit)
```

## Prerequisites

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* SQL Server - **LocalDB** (installed with Visual Studio) or any SQL Server instance; SSMS to inspect the data
* Visual Studio 2022 (or VS Code / `dotnet` CLI)

## Setup and run instructions

1. **Clone** the repository and open `RaceDayNova.sln` in Visual Studio (or open a terminal in the repo root).
2. **Connection string** - open `src/RaceDayNova.Api/appsettings.json`. The default targets LocalDB and creates a database called
   `RaceDayNovaApiDB`:
   ```json
   "RaceDayNovaDb": "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayNovaApiDB;Trusted_Connection=True;TrustServerCertificate=True"
   ```
   Use a different server by changing `Server=`. (Use a **new** database name rather than the Part 1 `RaceDayNovaDB`, which already
   contains tables created by the SQL script.)
3. **Create the database (code-first).** Install the EF tool once, then create and apply the initial migration:
   ```bash
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add InitialCreate --project src/RaceDayNova.Api
   dotnet ef database update --project src/RaceDayNova.Api
   ```
   Commit the generated `Migrations` folder. (If you skip this step the API still builds the schema from the model on first start.)
   The database is seeded with the same data as the Part 1 script.
4. **Run the API:**
   ```bash
   dotnet run --project src/RaceDayNova.Api --launch-profile https
   ```
   Open **https://localhost:7080/swagger** (or press F5 in Visual Studio - it opens Swagger automatically).
5. **Run the tests:**
   ```bash
   dotnet test
   ```
   The tests use an in-memory database and a fake weather provider, so they need no SQL Server and no internet.

### Demo accounts (seeded, password `Password123!`)

| Role | Email |
|---|---|
| Organiser | `thandi.maseko@racedaynova.co.za` (owns events 2001 and 2003) |
| Organiser | `johan.botha@racedaynova.co.za` (owns event 2002) |
| Participant | `lebo.molefe@racedaynova.co.za` |
| Participant | `zanele.khumalo@racedaynova.co.za` |

Organiser invite code for registering new Organisers (config key `Registration:OrganiserInviteCode`): `RACEDAY-ORG-2026`.

### Trying it in Swagger

1. `POST /api/auth/login` with a demo account. The browser stores the session cookie automatically.
2. Call any endpoint - each one lists who may use it. Log out with `POST /api/auth/logout` and log in as another role to see the 403 responses.

---

## The two roles

| | **Organiser** | **Participant** |
|---|---|---|
| Registration | Needs the organiser invite code | Open to anyone |
| Events | Create, edit and delete **their own** events | Browse and view events |
| Categories | Create / edit / delete categories; add them to their events | View categories |
| Enrolments | View all enrolments for **their** events | Enter an event by choosing a category; view / cancel **their own** enrolments |
| Results | Capture and correct finish time, position and pace for **their** events | View **their own** results |
| Profile | View / update own profile | View / update own profile |

Organiser functionality is closed to Participants and vice versa: the API answers **403 Forbidden** to a logged-in user with the wrong role
(or who does not own the resource) and **401 Unauthorized** to anyone not logged in.

## Authentication design

* **Register** `POST /api/auth/register` - personal details plus a role. Organiser accounts are protected by an invite code held in server
  configuration, so a user cannot simply choose to become an Organiser. Passwords must be 8+ characters with a letter and a digit.
* **Passwords are hashed** with salted PBKDF2 (HMAC-SHA256, 210 000 iterations) - the plain text is never stored.
* **Login** `POST /api/auth/login` creates a **server-side session** that stores the user's **id and role**; the browser receives only an opaque,
  `HttpOnly` session cookie. Every later request is authorised from the session by the `[SessionAuthorize]` attribute - nothing the client sends
  can change its role.
* **Logout** `POST /api/auth/logout` destroys the session. Sessions expire after 60 idle minutes.

## Endpoints

All endpoints are visible and testable in Swagger, each with a description, expected input and responses.

| Method | Route | Access |
|---|---|---|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/logout` | Public |
| GET / PUT | `/api/users/me` | Any logged-in user |
| GET | `/api/events` (filters: eventType, city, province, status, fromDate, toDate, search, organiserId) | Public |
| GET | `/api/events/{id}` | Public |
| POST | `/api/events` | Organiser |
| PUT / DELETE | `/api/events/{id}` | Organiser (owner) |
| GET | `/api/categories` | Public |
| POST | `/api/categories` | Organiser |
| PUT / DELETE | `/api/categories/{id}` | Organiser |
| GET | `/api/events/{id}/categories` | Public |
| POST | `/api/events/{id}/categories` | Organiser (owner) |
| DELETE | `/api/events/{id}/categories/{categoryId}` | Organiser (owner) |
| POST | `/api/events/{id}/enrolments` | Participant |
| GET | `/api/enrolments/me` | Participant |
| GET | `/api/events/{id}/enrolments` | Organiser (owner) |
| DELETE | `/api/enrolments/{id}` | Participant (own) |
| POST | `/api/enrolments/{id}/result` | Organiser (owner) |
| PUT | `/api/results/{id}` | Organiser (owner) |
| GET | `/api/results/me` | Participant |
| GET | `/api/events/{id}/results` | Organiser (owner) |
| GET | `/api/events/{id}/route` | Public |
| GET | `/api/events/{id}/weather` | Public |
| GET | `/api/health` | Public |

See [`docs/Part2_API_Implementation_Notes.md`](docs/Part2_API_Implementation_Notes.md) for business rules, status codes and the deviations from the Part 1 plan.

## Unit tests

`tests/RaceDayNova.Tests` (xUnit) starts the real API in memory and calls it over HTTP. It covers success **and** failure scenarios for:

* **Registration and login** - valid/duplicate/invalid registration, organiser invite code, wrong password, session cookie, logout, password hashing
* **Event management** - Organisers create / update / delete; Participants and anonymous callers rejected; ownership enforced
* **Role enforcement** - a matrix of every Organiser-only, Participant-only and logged-in-only endpoint tested as the wrong role (403) and anonymous (401)
* **Event enrolments** - enrolling, recording of participant + event + category, duplicates, closed events, full categories, cancelling, re-enrolling
* **Categories, results, profile, route, weather, health and Swagger** (checks every planned endpoint appears in the Swagger document)

## CI/CD

`.github/workflows/part2-ci.yml` runs on every push and pull request. It (1) **validates the repository structure** (required files, folders and README
sections), then (2) restores, builds in Release and **runs all tests**, uploading the results.

**Green build screenshot:**

 <img width="1350" height="584" alt="image" src="https://github.com/user-attachments/assets/c76ecd99-6329-4340-a162-a265a1b0b79a" />


## Video

Unlisted YouTube video demonstrating the running API, code structure, endpoints, authentication and unit tests:
`PASTE YOUR UNLISTED YOUTUBE LINK HERE`

## Design notes

* **Code-first, matching Part 1:** table/column names, types, keys, unique indexes, defaults, CHECK constraints and cascade rules in
  `RaceDayDbContext` mirror `docs/RaceDay_Nova_Database.sql` and the ERD (seven entities).
* **Consistent errors:** failures return RFC 7807 problem details with the right status (400, 401, 403, 404, 409, 503).
* **Data integrity:** one active enrolment per participant per event, category capacity enforced, unique finishing positions per category,
  events with enrolments are cancelled rather than erased so results history is preserved.
