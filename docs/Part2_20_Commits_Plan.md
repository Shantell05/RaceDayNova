# Part 2 - 20 meaningful commits

Make these on your own GitHub account, on a branch named `part2-api`, as you work through the project (build each piece, run it, then commit).
Rubric: **20 or more meaningful commits**, a green workflow run, and a screenshot in the README.

| # | Commit message | What goes in it |
|---|---|---|
| 01 | Create Part 2 solution and Web API project | `.sln`, `RaceDayNova.Api.csproj`, `Program.cs`, `.gitignore` |
| 02 | Add entity models and constants | `Models/` (seven entities, roles, statuses) |
| 03 | Add DbContext with Fluent API mapping | `RaceDayDbContext.cs` (keys, types, FKs, indexes, CHECKs) |
| 04 | Add seed data from Part 1 script | `SeedData.cs` |
| 05 | Configure SQL Server connection and create initial migration | `appsettings.json`, `Migrations/` |
| 06 | Add PBKDF2 password hasher | `Security/PasswordHasher.cs`, `PasswordPolicy.cs` |
| 07 | Add session management and SessionAuthorize attribute | `AddSession` in `Program.cs`, `SessionAuthorizeAttribute.cs` |
| 08 | Implement register, login and logout | `AuthController`, auth DTOs |
| 09 | Implement user profile endpoints | `UsersController` |
| 10 | Implement event endpoints | `EventsController` (CRUD, filters), event DTOs |
| 11 | Implement category endpoints | `CategoriesController` |
| 12 | Implement event category endpoints | `EventCategoriesController` |
| 13 | Implement enrolment endpoints | `EnrolmentsController` |
| 14 | Implement result endpoints | `ResultsController` |
| 15 | Add route, weather and health endpoints | weather service, `HealthController` |
| 16 | Integrate Swagger with XML documentation | Swagger setup, `SessionAccessOperationFilter` |
| 17 | Add test project with in-memory API factory | `ApiFactory`, `TestHelpers` |
| 18 | Add authentication and role enforcement tests | `AuthTests`, `RoleEnforcementTests`, `PasswordHasherTests` |
| 19 | Add event, category, enrolment and result tests | remaining test classes |
| 20 | Add GitHub Actions workflow, README and notes | `part2-ci.yml`, README, `docs/` |

Tip: if a step is large, split it into two real commits (for example "enrolment rules" and "enrolment endpoints"). Do not make
empty or cosmetic commits - the rubric asks for *meaningful* ones.
