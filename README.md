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
- ASP.NET Core Web API (controllers)
- [MediatR](https://github.com/jbogard/MediatR) 12.x (Apache 2.0): CQRS dispatching and pipeline behaviors
- [FluentValidation](https://docs.fluentvalidation.net/): input validation
- Testing: xUnit, NSubstitute, Shouldly

_The stack will be updated as new phases are completed._

## Solution Structure

```
.
├── BookingSystem.sln
├── Directory.Build.props          # Shared build settings for all projects
├── .editorconfig                  # Code style and naming conventions
├── src
│   ├── BookingSystem.Domain           # Entities, value objects, domain rules (no dependencies)
│   ├── BookingSystem.Application      # Use cases, ports/abstractions (-> Domain)
│   ├── BookingSystem.Infrastructure   # Persistence and external services (-> Application)
│   └── BookingSystem.Api              # HTTP entry point, composition root (-> Application, Infrastructure)
└── tests
    ├── BookingSystem.UnitTests            # Domain and Application tests
    ├── BookingSystem.IntegrationTests     # End-to-end tests through the API
    └── BookingSystem.ArchitectureTests    # Enforces the dependency rule between layers
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

## Getting Started

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```bash
dotnet build
dotnet test
dotnet run --project src/BookingSystem.Api
```

## Status

🚧 **In progress — Phase 2: Application completed**

- [x] Phase 0: Solution setup
- [x] Phase 1: Domain layer and unit tests
- [x] Phase 2: Application layer (CQRS use cases, pipeline behaviors) and unit tests
