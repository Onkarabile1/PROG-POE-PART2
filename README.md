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

The API uses JWT bearer authentication. A user registers, logs in to receive a token, and sends that token with every protected request.

### How it works
1. `POST /api/auth/register` creates a user with the role `Organiser` or `Participant`. The email must be unique.
2. `POST /api/auth/login` checks the email and password and returns a JWT plus the user's profile. The token is valid for 8 hours.
3. Protected endpoints need the header `Authorization: Bearer <token>`.

The token carries the user's id, email, role and profile id as claims. Controllers use the role claim to decide who can do what.

### Passwords
Passwords are never stored in plain text. They are hashed with ASP.NET Core's `PasswordHasher` before they are saved in the `Users` table, and login compares the hash. A failed login returns the same message whether the email or the password was wrong.

### Roles
| Role | Can do |
|---|---|
| Organiser | Create and manage events, categories, sponsors and weather logs, and capture and correct results |
| Participant | Browse events, enrol in categories, pay, and view their own enrolments and results |

Both roles can view and update their own profile and change their own password.

### Auth endpoints
| Method | Endpoint | Access |
|---|---|---|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| GET | `/api/users/me` | Any logged-in user |
| PUT | `/api/users/me` | Any logged-in user |
| PUT | `/api/users/me/password` | Any logged-in user |

### Trying it in Swagger
1. Register a user, then log in and copy the `token` from the response.
2. Click **Authorize** at the top of the Swagger page and paste the token.
3. Call `GET /api/users/me` to confirm you are logged in.

### Configuration
JWT settings are in the `Jwt` section of `appsettings.json` (`Key`, `Issuer`, `Audience`). The key there is a development placeholder. In a real deployment it would be stored in a secret store or environment variable, not in source control.

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

## External weather service
`POST /api/events/{eventId}/weather/refresh` calls the free [Open-Meteo](https://open-meteo.com) API (no API key needed). The event's city is looked up in South Africa, using the event's province to choose between places with the same name, and the current temperature, wind speed and conditions are stored as a new `WeatherLog` row.

## Swagger
Run the API and open `/swagger`. Every endpoint shows its purpose, who may call it, the data it expects and the responses it can return. Click **Authorize** and paste the token from `POST /api/auth/login` to test the protected endpoints.

## Running the tests
The tests are in `RaceDay.Tests` (xUnit). They start the real API in memory with `WebApplicationFactory` and use an in-memory database, so no SQL Server is needed.

- **Visual Studio:** Test > Run All Tests
- **Command line:** `dotnet test`

| Area | What is tested |
|---|---|
| Registration and login | Success for both roles; invalid role, email and password; duplicate email; wrong password; the password is stored hashed; the token works on protected endpoints |
| Event management | Organisers create, update and delete their own events; invalid event type, name and date are rejected; another Organiser gets 403; events with enrolments cannot be deleted |
| Categories | Create with route details, ownership checks, participant limit and delete rules |
| Role access | Every protected endpoint returns 401 without a token; Organiser-only and Participant-only endpoints return 403 for the wrong role; public endpoints stay open; a forged token is rejected |
| Event enrolments | A Participant can enrol and the participant, event, category, bib number and pending payment are recorded; duplicates and full categories return 409; cancelling and re-enrolling |
| Payments and results | Paying the exact fee, refunds, capturing and correcting results, and the public leaderboard |

The in-memory database does not enforce SQL Server constraints such as unique indexes, so the rules the tests check are the ones the controllers enforce.

Any further deviations will be listed here.


Area	Change	Reason
Events	DELETE /api/events/{id} returns 409 Conflict if the event has enrolments	Enrolments use a Restrict delete rule, so removing the event would otherwise fail with a database error
Events	GET /api/events?date= matches that exact day. Without it, only upcoming events are listed	The plan didn’t say how the date filter works
Categories	PUT /api/categories/{id} returns 409 Conflict if the new participant limit is below the number already enrolled	Prevents a category being over-full


Enrolments	Bib numbers have the form E{eventId}-{running number}, for example E1-0001	The ERD only says the bib number is unique
Enrolments	Re-enrolling after a cancellation reuses the cancelled record, and a Refunded or Pending payment is reset to Pending	A participant can only have one enrolment per category (unique index)
Enrolments	Cancelling does not change the payment. An organiser refunds it with PUT /api/payments/{id}	The plan has a separate refund endpoint
Payments	POST must send an amount equal to the category’s entry fee (otherwise 400). A cancelled enrolment returns 409	The plan says the payment is “for the entry fee”

Results	Status must be Finished, DNF or DNS. A Finished result needs a finish time and a position. A DNF or DNS result must not have either (400)	Keeps the leaderboard consistent
Results	Capturing or correcting a result returns 409 if the position is already taken in that category, and capturing one for a cancelled enrolment also returns 409	Two people cannot share a position
Results	The leaderboard lists finishers by position first, then DNF and DNS	The plan only says “ordered by position”

Sponsors	Linking a sponsor to an event returns 404 if the sponsor doesn’t exist, and GET /api/events/{eventId}/sponsors returns 404 if the event doesn’t exist	The plan listed only the 403 and 409 cases for these
Sponsors	sponsorshipTier is required when linking, and contactEmail and logoUrl must be a valid email and URL	Basic input validation

Weather	The refresh stores the current conditions at the event’s city, not a forecast for the event date	The plan describes a snapshot logged over time
Weather	Refresh returns 400 if the event has no city and 404 if the city can’t be found, as well as the planned 404 (event missing) and 502 (service unavailable)	The weather lookup depends on the event’s city