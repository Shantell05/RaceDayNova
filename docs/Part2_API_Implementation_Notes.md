# RaceDay Nova - Part 2 Implementation Notes

How the built API traces back to the Part 1 plan and to the Part 2 specification and rubric.

## 1. Deviations from the Part 1 endpoint plan (and why)

| Part 1 plan | Part 2 implementation | Reason |
|---|---|---|
| "Secure token-based mechanism"; login returns a token | **Server-side session**; login sets an HttpOnly session cookie holding user id + role | The Part 2 brief requires session management and the rubric requires a server-side session that stores user ID and role |
| Register creates a **participant** account only (`name, email, password, mobile`) | Register takes `firstName, lastName, email, password, mobile, role` | Part 2 says users select their role when registering; `name` is split to match `FirstName` / `LastName` in the ERD |
| Organiser creation not described | Organiser registration needs `organiserInviteCode` (server config) | Part 2 says "think about how this should be managed" - an open role dropdown would let anyone become an Organiser |
| `POST .../categories` body `categoryId, fee, capacity` | `categoryId, entryFee, placesAvailable` | Property names aligned with the `EventCategories` columns |
| `DELETE /api/events/{id}` "Delete/cancel" | Hard delete when no enrolments, otherwise status becomes `Cancelled` and enrolments are cancelled | Foreign keys protect enrolment/result history |
| `DELETE /api/events/{id}/categories/{categoryId}` 204/403/404 | Adds **409** when participants are already enrolled | Prevents orphaning enrolments |
| `DELETE /api/enrolments/{id}` 204/403/404 | Soft-cancel (status `Cancelled`), **409** if a result exists | Keeps history |
| - | Added `POST /api/auth/logout` | A session needs a way to end |

All 27 endpoints in `RaceDay_Nova_API_Endpoint_Plan.md` are implemented with the planned routes and methods.

## 2. Business rules

* **Ownership:** Organisers can only edit / delete / manage categories / view enrolments / capture results for events they organise (403 otherwise).
* **Enrolment:** event status must not be Closed, Completed or Cancelled; category must be offered by the event (400); one active enrolment per
  participant per event (409); `PlacesAvailable` is the category capacity (409 when full); re-enrolling after cancelling re-activates the row
  because the table has `UNIQUE(ParticipantId, EventCategoryId)`.
* **Results:** one per enrolment (409 on duplicate); `Finished` needs a finish time; positions must be unique per event category; pace is
  calculated from finish time and distance when not supplied; DNF / DNS / DSQ cannot carry a position.
* **Weather:** stored snapshot returned if under 3 hours old; otherwise Open-Meteo is called and a new snapshot saved; falls back to the stored
  snapshot, then 503.

## 3. Rubric traceability (100 marks)

| Rubric area | Marks | Where it is met |
|---|---|---|
| Correct submission | 5 | Clear folder structure, detailed README with setup steps, CI screenshot slot and video link slot |
| GitHub and CI/CD | 10 | `.github/workflows/part2-ci.yml` validates repository structure, builds and tests; 20+ commits (see `Part2_20_Commits_Plan.md`) |
| Authentication and role-based access | 20 | `AuthController`, `Pbkdf2PasswordHasher`, `SessionAuthorizeAttribute`; session stores UserId + UserRole; every protected endpoint enforces role |
| API endpoints and database | 25 | All planned endpoints; EF Core code-first model equal to the Part 1 ERD / SQL (`RaceDayDbContext`, `SeedData`) |
| Swagger integration | 10 | Swashbuckle UI at `/swagger`; XML summaries, remarks, request/response types and an automatic "Access" line per endpoint |
| Unit testing | 15 | `tests/RaceDayNova.Tests` - success and failure paths for auth, events, categories, enrolments, results and a full role matrix |
| Video presentation | 15 | See `Part2_Video_Script.md` |

## 4. Schema notes

* The unique **constraints** in the SQL script (`UQ_Users_Email`, etc.) are created by EF Core as unique **indexes** with the same names - the
  behaviour is identical.
* `IX_Results_Enrolment` from the SQL script is omitted because `UQ_Results_Enrolment` already indexes the same column.
* The seeded users' `PasswordHash` values are real PBKDF2 hashes (the Part 1 script has placeholders). `docs/sql/Part2_Update_Password_Hashes.sql`
  applies them to a database created from the Part 1 script.
