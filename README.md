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

### Prerequisites
- Visual Studio 2022 with the ASP.NET and web development workload
- .NET 8 SDK
- SQL Server (SQL Server Express) and SQL Server Management Studio

### Setup
1. Clone the repo and open `RaceDay.API.sln` in Visual Studio.
2. Open `RaceDay.API/appsettings.json` and update the connection string to match your SQL Server instance:
```
   "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=RaceDayApiDb;Trusted_Connection=True;TrustServerCertificate=True;"
```
   I developed this on `LabVM2049939\SQLEXPRESS`. On another machine this will be different, for example `localhost\SQLEXPRESS`. Remember to use a double backslash (`\\`) in the JSON file.
3. Open **Tools > NuGet Package Manager > Package Manager Console** and run:
```
   Update-Database
```
   This applies the `InitialCreate` migration and creates the database with all 11 tables (Users, Organisers, Participants, Events, Categories, Enrolments, Payments, Results, Sponsors, EventSponsors, WeatherLogs).
4. Press **F5** to run the API. Swagger opens at `https://localhost:7286/swagger`. The port number may differ on your machine.

### Database
The database is created by Entity Framework Core migrations (code first). The migration files are in `RaceDay.API/Migrations`. The Part 1 SQL scripts were written for MySQL and are not used in this part.

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