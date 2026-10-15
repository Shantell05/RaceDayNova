# Part 2 video script (record in your own voice - AI voices are not allowed)

Upload as an **unlisted** YouTube video and paste the link into the README. Aim for 8-12 minutes.

1. **Intro (0:30)** - RaceDay Nova, what the API does, the two roles.
2. **Code structure (1:30)** - walk the folders: Controllers, Models, Data, Dtos, Security, Services, tests, workflow.
3. **Database (1:00)** - show `RaceDayDbContext` mapping to the Part 1 ERD; show tables and seed data in SSMS.
4. **Running the API (0:30)** - `dotnet run`, open Swagger, point out every endpoint is listed with its description and access line.
5. **Authentication (2:00)** - register a Participant; try registering an Organiser without the code (403) then with it;
   log in; explain the server-side session (user id + role) and the hashed password in the database.
6. **Endpoints demo (3:00)** - as Organiser: create an event, add a category. Switch to a Participant: enrol. Back as Organiser: view enrolments,
   capture a result. As Participant: view own results. Show a 403 (Participant creating an event) and a 401 (logged out).
7. **Unit tests (1:30)** - run `dotnet test`; explain the role-enforcement matrix and enrolment tests.
8. **CI/CD (0:45)** - GitHub Actions run, structure validation job, green build, commit history.
9. **Wrap-up (0:15)** - how this prepares the MVC front end in Part 3.
