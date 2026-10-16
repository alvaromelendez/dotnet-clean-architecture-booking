# BookingSystem

A space reservation management system for booking rooms, sports courts, medical offices and other shared spaces.

## Purpose

This is a portfolio project that demonstrates how to build a maintainable, well-tested .NET application using:

- **Clean Architecture** — strict, inward-only dependency rule between layers.
- **Domain-Driven Design (DDD)** — a rich, framework-agnostic domain model.
- **CQRS** — clear separation between commands (writes) and queries (reads).
- **Testing** — unit, integration and architecture tests.

## Tech Stack

- [.NET 10](https://dotnet.microsoft.com/) / C# (latest)
- ASP.NET Core Minimal APIs with RFC 9457 problem details, native rate limiting and health checks
- [Asp.Versioning](https://github.com/dotnet/aspnet-api-versioning) (URL segment `/api/v1`) and native OpenAPI (`Microsoft.AspNetCore.OpenApi`) with the [Scalar](https://scalar.com/) UI
- [MediatR](https://github.com/jbogard/MediatR) 12.x (Apache 2.0): CQRS dispatching and pipeline behaviors
- [FluentValidation](https://docs.fluentvalidation.net/): input validation
- [PostgreSQL](https://www.postgresql.org/) with [EF Core 10](https://learn.microsoft.com/ef/core/) and [Npgsql](https://www.npgsql.org/efcore/) (snake_case naming via EFCore.NamingConventions)
- Testing: xUnit, NSubstitute, Shouldly, [Testcontainers](https://dotnet.testcontainers.org/) (real PostgreSQL in integration tests),
  [ArchUnitNET](https://github.com/TNG/ArchUnitNET) (architecture rules), [FsCheck](https://fscheck.github.io/FsCheck/) (property-based tests),
  coverlet + [ReportGenerator](https://reportgenerator.io/) (coverage) and [Stryker.NET](https://stryker-mutator.io/) (mutation testing)

_The stack will be updated as new phases are completed._

## Solution Structure

```
.
├── BookingSystem.sln
├── Directory.Build.props          # Shared build settings for all projects
├── .editorconfig                  # Code style and naming conventions
├── docker-compose.yml             # Local PostgreSQL (credentials from a git-ignored .env)
├── .env.example                   # Template for .env
├── .config/dotnet-tools.json      # Pinned local tools (dotnet-ef, ReportGenerator, Stryker.NET)
├── coverlet.runsettings           # Coverage exclusions
├── stryker-config.*.json          # Mutation testing, one config per mutated project
├── scripts                        # coverage.sh/.ps1 and mutation.sh/.ps1
├── docs/testing                   # Test strategy and mutation notes
├── src
│   ├── BookingSystem.Domain           # Entities, value objects, domain rules (no dependencies)
│   ├── BookingSystem.Application      # Use cases, ports/abstractions (-> Domain)
│   ├── BookingSystem.Infrastructure   # Persistence and external services (-> Application)
│   └── BookingSystem.Api              # HTTP entry point, composition root (-> Application, Infrastructure)
└── tests
    ├── BookingSystem.UnitTests            # Domain and Application tests, incl. property-based tests
    ├── BookingSystem.IntegrationTests     # PostgreSQL (Testcontainers) and end-to-end HTTP tests
    ├── BookingSystem.ArchitectureTests    # Dependency rule and conventions (ArchUnitNET)
    └── BookingSystem.TestUtilities        # Test data builders shared by unit and integration tests
```

### Dependency Rule

```
Api ──► Infrastructure ──► Application ──► Domain
 └────────────────────────►┘
```

`BookingSystem.Domain` has no project or NuGet package references, keeping the core of the system fully independent of frameworks and infrastructure.

### Application Layer: CQRS and Pipeline

Use cases are organized as **vertical slices** (`Rooms/CreateRoom`, `Bookings/CancelBooking`, ...). Each slice holds its request, handler, validator and response.

- **Commands** (`ICommand`, `ICommand<T>`) change state. Handlers stay thin: they load the aggregate, call a domain method, and persist through `IUnitOfWork` only when it succeeds.
- **Queries** (`IQuery<T>`) read through dedicated read-side interfaces (`IRoomQueries`, `IBookingQueries`) that return DTOs directly, without loading aggregates.
- Every handler returns `Result`/`Result<T>`. Expected failures (not found, validation, conflicts) never throw. Each error carries an `ErrorType` that the API will map to HTTP status codes.
- The current time comes from `IDateTimeProvider` and is passed into the domain.
- Domain events reach MediatR through `DomainEventNotification<T>`, so the domain stays free of MediatR.

Each request goes through this pipeline:

```
Request ──► LoggingBehavior ──► ValidationBehavior ──► Handler ──► Domain
```

| Behavior | Responsibility |
|---|---|
| `LoggingBehavior` | Logs the request name, duration and error code on failure. It never logs the request payload, which may contain sensitive data. |
| `ValidationBehavior` | Runs the FluentValidation validators. If any fail, it returns a failed `Result` with a `ValidationError` grouped by property, without throwing and without invoking the handler. |

Validators check only the **shape** of the input: non-empty ids and dates, page ranges, and 3-letter currency codes. Business rules are not duplicated there; they live in the domain.

## Infrastructure Layer: Persistence

`BookingSystem.Infrastructure` implements the Application ports with EF Core and PostgreSQL:

- **Mappings** use the Fluent API (`IEntityTypeConfiguration<T>`), so the domain carries no persistence attributes.
  - Strongly typed ids are stored as `uuid` through value converters.
  - `Money` and `TimeSlot` are owned types.
  - Booking status is stored as text.
- **Write side:** repositories track aggregates. Only the unit of work commits.
- **Read side:** query classes use `AsNoTracking` and project straight to DTOs in SQL.
  - Availability is a `NOT EXISTS` subquery.
  - User bookings are paginated with a stable order (`created_at_utc DESC, id`).

### Preventing double bookings

Availability is checked twice:

1. **In the domain.** `BookingService.Reserve` rejects overlaps with the active bookings it is given.
2. **In the database.** Two concurrent requests can both pass step 1. A GiST **exclusion constraint** settles the race:

   ```sql
   EXCLUDE USING gist (room_id WITH =, tstzrange(start_utc, end_utc, '[)') WITH &&)
   WHERE (status IN ('Pending', 'Confirmed'))
   ```

   The `[)` bounds mirror the domain's half-open `TimeSlot`, so back-to-back bookings are allowed. The unit of work translates the violation (SQLSTATE `23P01`) into a `ConcurrencyConflictException`. `CreateBooking` turns that into a `Booking.SlotNoLongerAvailable` conflict.

Concurrent edits of the same aggregate are caught by **optimistic concurrency** on PostgreSQL's `xmin` system column, mapped as a shadow row-version property. Room names are unique regardless of case through a unique index on `lower(name)`.

### Domain events

`UnitOfWork.SaveChangesAsync` handles events in three steps:

1. It collects the events of tracked aggregates and clears them.
2. It commits.
3. Only after the commit, it publishes each event through MediatR, wrapped in `DomainEventNotification<T>`.

Delivery is **at-most-once**. A crash between the commit and the publish loses the events. The robust alternative is the Transactional Outbox pattern, which is out of scope for this project.

## Domain Model

The domain layer is pure C#: no project or NuGet references, and no access to the system clock.
Every time-dependent operation receives `DateTime utcNow` as a parameter. Expected business failures
are returned as `Result`/`Result<T>` with a typed `Error` (codes such as `Booking.RoomInactive`);
exceptions are reserved for programming errors.

### Building blocks

- `Entity<TId>` / `AggregateRoot<TId>` with identity equality and domain events.
- Strongly typed ids (`RoomId`, `BookingId`, `UserId`) that reject `Guid.Empty`.
- Value objects:
  - `Money`: a non-negative amount with an ISO 4217 currency. Mixing currencies throws, because the domain has no exchange rates.
  - `TimeSlot`: a half-open UTC interval `[Start, End)` lasting 30 minutes to 8 hours. Adjacent slots do not overlap.

### Room

- Name is required (max 100 characters), capacity must be greater than zero, and the room has an hourly rate.
- `Activate` / `Deactivate` return an error if the room is already in that state. Deactivating raises `RoomDeactivatedDomainEvent`.
- The price of a slot is the hourly rate prorated to its exact duration.

### Booking

```
Pending ──► Confirmed ──► Completed
   │            │
   └────────────┴──► Cancelled
```

- Bookings are created only through `BookingService.Reserve`, which enforces:
  1. The room is active.
  2. Attendees are between 1 and the room capacity.
  3. The slot starts in the future.
  4. The slot starts no more than 100 days ahead.
  5. The slot does not overlap a pending or confirmed booking of the same room.
- New bookings are `Pending` and priced from the room's hourly rate.
- A booking can be completed only after its slot ends. `Cancelled` and `Completed` are terminal states. Invalid transitions return errors.
- Each transition raises a domain event (`BookingCreated`, `BookingConfirmed`, `BookingCancelled`, `BookingCompleted`).

### Cancellation policy

| Notice before start | Refund |
|---|---|
| 24 hours or more | 100 % |
| 2 hours to under 24 hours | 50 % |
| Under 2 hours | 0 % |

Pending bookings were never charged, so they are refunded nothing. A booking cannot be cancelled once its slot has started.

## API

All business endpoints live under `/api/v1`. Every error is an `application/problem+json` response (RFC 9457) with a stable `code` and a `traceId` that matches the server logs.

| Method | Route | Description | Success |
|---|---|---|---|
| `POST` | `/api/v1/rooms` | Create a room | `201` + `Location` |
| `GET` | `/api/v1/rooms?isActive=` | List rooms | `200` |
| `GET` | `/api/v1/rooms/{id}` | Get a room | `200` |
| `PUT` | `/api/v1/rooms/{id}` | Update a room | `204` |
| `POST` | `/api/v1/rooms/{id}/activate` | Activate a room | `204` |
| `POST` | `/api/v1/rooms/{id}/deactivate` | Deactivate a room | `204` |
| `GET` | `/api/v1/rooms/available?start=&end=&minCapacity=` | Rooms free for a slot | `200` |
| `POST` | `/api/v1/bookings` | Book a room | `201` + `Location` |
| `GET` | `/api/v1/bookings/{id}` | Get a booking | `200` |
| `POST` | `/api/v1/bookings/{id}/confirm` | Confirm a booking | `204` |
| `POST` | `/api/v1/bookings/{id}/cancel` | Cancel and get the refund | `200` |
| `POST` | `/api/v1/bookings/{id}/complete` | Complete a booking | `204` |
| `GET` | `/api/v1/users/{userId}/bookings?status=&page=&pageSize=` | A user's bookings (paged) | `200` |
| `GET` | `/health/live` | Liveness (no dependencies) | `200` |
| `GET` | `/health/ready` | Readiness (includes PostgreSQL) | `200` |

How errors map to HTTP status codes:

| Error type | Status |
|---|---|
| `Validation` | `400`. Input-shape failures include an `errors` object keyed by field. |
| `NotFound` | `404` |
| `Conflict` | `409`. This includes overlaps, invalid state transitions and concurrent modifications. |
| `Failure` or an unhandled exception | `500`. A generic message only: no exception text or stack trace, in any environment. |
| Rate limit exceeded | `429`, with a `Retry-After` header |

Dates are accepted as ISO 8601 with any offset and are converted to UTC. Responses are always in UTC. In query strings, encode `+` as `%2B`.

## Getting Started

Prerequisites:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/). The integration tests start a disposable PostgreSQL container through Testcontainers.

```bash
dotnet build
dotnet test   # unit, architecture and integration tests (Docker must be running)
```

See [Testing](#testing) for running a single category, coverage and mutation testing.

### Running the API locally

1. **Start PostgreSQL** with Docker Compose. The credentials live in a git-ignored `.env` file:

   ```bash
   cp .env.example .env          # then replace POSTGRES_PASSWORD with your own value
   docker compose up -d
   ```

2. **Provide the connection string.** It is never stored in `appsettings*.json`. The API reads `ConnectionStrings:Database`, and you can supply it in either of two ways. Use the values from your `.env`.

   ```bash
   # Option A: user-secrets (Development only, stored outside the repository)
   dotnet user-secrets set "ConnectionStrings:Database" \
     "Host=localhost;Port=5432;Database=booking_system;Username=booking;Password=<your-password>" \
     --project src/BookingSystem.Api

   # Option B: environment variable (double underscore = section separator)
   export ConnectionStrings__Database="Host=localhost;Port=5432;Database=booking_system;Username=booking;Password=<your-password>"
   ```

3. **Run the API.** In Development, pending migrations are applied on startup (`Database:MigrateOnStartup`). Other environments never migrate automatically.

   ```bash
   dotnet run --project src/BookingSystem.Api --launch-profile http
   ```

4. **Explore it.** These pages are available in Development only:
   - Scalar UI: <http://localhost:5249/scalar>
   - OpenAPI document: <http://localhost:5249/openapi/v1.json>
   - Ready-made requests: `src/BookingSystem.Api/BookingSystem.Api.http`

Rate limits are configured in the `RateLimiting` section of `appsettings.json`: a fixed window per client IP.

### Database migrations

The EF Core CLI is pinned as a local tool (`.config/dotnet-tools.json`):

```bash
dotnet tool restore

# Add a migration
dotnet ef migrations add <Name> --project src/BookingSystem.Infrastructure --output-dir Persistence/Migrations

# Apply migrations (the design-time factory reads this variable)
export BOOKINGSYSTEM_DB_CONNECTION="Host=localhost;Port=5432;Database=booking_system;Username=<user>;Password=<password>"
dotnet ef database update --project src/BookingSystem.Infrastructure
```

## Testing

The suite has three categories, one per test project: **Unit** (316 tests, including property-based tests),
**Integration** (86, real PostgreSQL through Testcontainers) and **Architecture** (39). No test reads the
system clock or depends on execution order. The full strategy, the reasons behind the thresholds and the
current results are in [docs/testing/strategy.md](docs/testing/strategy.md).

| Command | Purpose |
|---|---|
| `dotnet test` | Run every test (Docker must be running) |
| `dotnet test --filter "Category=Unit"` | Unit tests only |
| `dotnet test --filter "Category=Integration"` | Integration tests only (Docker) |
| `dotnet test --filter "Category=Architecture"` | Architecture rules only |
| `scripts/coverage.sh` (`scripts/coverage.ps1` on Windows) | All tests with coverage. Writes `coverage-report/` (HTML, Cobertura, Markdown) and fails below 95 % (Domain), 90 % (Application) or 85 % (global) line coverage |
| `scripts/mutation.sh` (`scripts/mutation.ps1`) | Stryker.NET on Domain and Application with the unit tests. Writes `StrykerOutput/<project>/reports/` and fails below the `break` threshold. Notes: [docs/testing/mutation-notes.md](docs/testing/mutation-notes.md) |

The tools are pinned locally; run `dotnet tool restore` once.

## Status

🚧 **In progress — Phase 5: Testing completed**

- [x] Phase 0: Solution setup
- [x] Phase 1: Domain layer and unit tests
- [x] Phase 2: Application layer (CQRS use cases, pipeline behaviors) and unit tests
- [x] Phase 3: Infrastructure layer (PostgreSQL, EF Core, unit of work) and integration tests
- [x] Phase 4: API layer (Minimal APIs, problem details, versioning, OpenAPI, health checks, rate limiting) and HTTP integration tests
- [x] Phase 5: Testing (architecture rules, test data builders, property-based tests, coverage and mutation thresholds)

## Known Limitations

- **No authentication or authorization yet.** `POST /bookings` takes the `userId` in the request body, and any caller can act on any booking or list any user's bookings. The user id should come from an authenticated principal, with ownership checks.
- **Domain events are dispatched at-most-once.** They are published after the database commit without a Transactional Outbox, so a crash between the commit and the publish loses them (for example, a confirmation notification). Notifications are only a logging stub today.
- **Room name races surface as `500`.** Two simultaneous requests creating rooms with the same name: the unique index rejects the second, but that violation is not translated to `409` yet.
- **The rate limiter is in-memory and per instance.** Partitioning is by client IP; behind a proxy this needs forwarded headers configured, and several instances do not share counters.
