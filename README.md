# RaceDay API (Part 2)

RESTful API that powers the RaceDay system. It handles all data operations, enforces role-based access for Organisers and Participants, and is tested and built automatically with GitHub Actions.

**Student:** [Your name] | **Student number:** [Your student number]
**Module:** [Module code and name] | **Part 1 repo:** [link to PROG-POE-PART1]

## Tech stack
- ASP.NET Core Web API (C#, .NET 8)
- Entity Framework Core with SQL Server
- Role-based authentication (Organiser / Participant)
- xUnit for unit tests
- GitHub Actions for CI/CD

## Project structure
```
RaceDay.API/
  Controllers/   API endpoints
  Data/          DbContext and migrations
  DTOs/          Request and response objects
  Models/        EF Core entities (from the Part 1 ERD)
  Services/      Business logic
RaceDay.Tests/   Unit tests
.github/workflows/  CI/CD pipeline
```

## Getting started
TODO: fill in after Step 2.

1. Clone the repo.
2. Update the connection string in `appsettings.json`.
3. Run `Update-Database` in the Package Manager Console.
4. Press F5 and open the Swagger page.

## API endpoints
The API follows the endpoint plan from Part 1 (`docs/api-endpoint-plan.md` in the Part 1 repo).

| Area | Endpoints | Status |
|---|---|---|
| Authentication | register, login | TODO |
| User profile | get/update profile, change password | TODO |
| Events | list, get, create, update, delete | TODO |
| Categories | list, get, create, update, delete | TODO |
| Enrolments | enrol, my enrolments, category enrolments, cancel | TODO |
| Payments | get, submit, update status | TODO |
| Results | capture, correct, my results, leaderboard | TODO |
| Sponsors | CRUD, link to events | TODO |
| Weather | latest, history, refresh | TODO |

## Authentication and roles
TODO: fill in after Step 3 (how login works, what Organiser and Participant can each do).

## Running the tests
TODO: fill in after Step 4.

## CI/CD
TODO: fill in after Step 5 (what the GitHub Actions workflow does and when it runs).

## Deviations from the Part 1 plan
The Part 1 ERD and endpoint plan disagree in a few places. The entities follow the endpoint plan where it needs extra fields.

| Entity | Change | Reason |
|---|---|---|
| Events | Added `City`, `Province`, `Venue` | Required by `POST /api/events` in the plan |
| Categories | Added `DistanceKm`, `StartPoint`, `EndPoint`, `ElevationGainM`, `MapUrl`. `EntryFee` is now decimal and `MaxParticipants` is int (ERD had VARCHAR). Replaces the ERD's `Destination` | Required by the category endpoints in the plan |
| WeatherLog | Added `LoggedAt` | Needed to return the latest snapshot and the history |
| Users | The login table (the second "Organisers" box in the ERD) is named `Users` | Matches the plan's `Users` record |
| Database | SQL Server with EF Core migrations instead of the MySQL scripts from Part 1 | Part 2 brief requires SQL Server |

Any further deviations will be listed here.