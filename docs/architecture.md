# Architecture

BookingSystem follows Clean Architecture: four projects, and every dependency points toward the domain.
The reasons behind each decision are recorded in the [ADRs](adr/README.md).

## Layers and dependency direction

```mermaid
flowchart LR
    subgraph Api["BookingSystem.Api (composition root)"]
        Endpoints["Minimal API endpoints<br/>problem details, versioning,<br/>rate limiting, health checks"]
    end

    subgraph Infrastructure["BookingSystem.Infrastructure"]
        Persistence["EF Core + Npgsql<br/>repositories, read queries,<br/>UnitOfWork, migrations"]
        Services["SystemDateTimeProvider<br/>LoggingNotificationSender"]
    end

    subgraph Application["BookingSystem.Application"]
        UseCases["Commands and queries (MediatR)<br/>Logging and Validation behaviors"]
        Ports["Ports: IRoomRepository, IBookingRepository,<br/>IRoomQueries, IBookingQueries, IUnitOfWork,<br/>IDateTimeProvider, INotificationSender"]
    end

    subgraph Domain["BookingSystem.Domain (no references)"]
        Model["Room, Booking, BookingService,<br/>CancellationPolicy, TimeSlot, Money,<br/>Result and Error"]
    end

    Api --> Application
    Api --> Infrastructure
    Infrastructure --> Application
    Application --> Domain
    Persistence -. implements .-> Ports
    Services -. implements .-> Ports
    Persistence --> DB[("PostgreSQL")]
```

Solid arrows are project references. Infrastructure implements the ports declared by Application (dotted arrows),
so Application never references EF Core or ASP.NET Core. `BookingSystem.ArchitectureTests` enforces these rules.

## Booking lifecycle

```mermaid
stateDiagram-v2
    [*] --> Pending: BookingService.Reserve<br/>(room active, capacity, future start,<br/>at most 100 days ahead, no overlap)
    Pending --> Confirmed: Confirm(utcNow)
    Pending --> Cancelled: Cancel(utcNow) before start<br/>refund = 0 (never charged)
    Confirmed --> Cancelled: Cancel(utcNow) before start<br/>refund 100% (24 h+), 50% (2 h+), else 0
    Confirmed --> Completed: Complete(utcNow) after end
    Cancelled --> [*]
    Completed --> [*]

    note right of Pending
        Pending and Confirmed are "active":
        they block the time slot, also in the
        database exclusion constraint.
    end note
```

Every other transition returns an error instead of throwing: `Booking.NotPending`, `Booking.NotCancellable`,
`Booking.NotConfirmed`, `Booking.NotYetEnded`, and `Booking.AlreadyStarted` when cancelling after the start.
Each successful transition raises a domain event (`BookingCreated`, `BookingConfirmed`, `BookingCancelled`,
`BookingCompleted`).

## Creating a booking

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant Endpoint as CreateBookingEndpoint
    participant Pipeline as MediatR pipeline<br/>(Logging, Validation)
    participant Handler as CreateBookingCommandHandler
    participant Rooms as RoomRepository
    participant Bookings as BookingRepository
    participant Service as BookingService (Domain)
    participant UoW as UnitOfWork
    participant DB as PostgreSQL

    Client->>Endpoint: POST /api/v1/bookings
    Endpoint->>Pipeline: Send(CreateBookingCommand)
    alt validator fails
        Pipeline-->>Endpoint: Result failure (ValidationError)
        Endpoint-->>Client: 400 validation problem
    end
    Pipeline->>Handler: Handle(command)
    Handler->>Rooms: GetByIdAsync(roomId)
    alt room not found
        Handler-->>Endpoint: Room.NotFound
        Endpoint-->>Client: 404 problem
    end
    Handler->>Bookings: GetActiveByRoomAsync(roomId, slot)
    Handler->>Service: Reserve(room, user, slot, attendees, existing, utcNow)
    alt rule broken (inactive, capacity, overlap, ...)
        Service-->>Handler: Result failure (e.g. Booking.Overlapping)
        Handler-->>Endpoint: failure
        Endpoint-->>Client: 400 or 409 problem
    end
    Service-->>Handler: Booking (Pending) + BookingCreatedDomainEvent
    Handler->>Bookings: AddAsync(booking)
    Handler->>UoW: SaveChangesAsync()
    UoW->>UoW: collect and clear domain events
    UoW->>DB: INSERT booking (one transaction)
    alt concurrent request committed an overlapping booking first
        DB-->>UoW: 23P01 exclusion_violation (ex_bookings_room_id_time_slot)
        UoW-->>Handler: ConcurrencyConflictException
        Handler-->>Endpoint: Booking.SlotNoLongerAvailable
        Endpoint-->>Client: 409 problem
    else committed
        DB-->>UoW: OK
        UoW->>Pipeline: Publish(DomainEventNotification<BookingCreatedDomainEvent>)
        Note over UoW,Pipeline: After the commit, at-most-once (ADR 0006).<br/>A failing handler is logged, not rethrown.
        UoW-->>Handler: saved
        Handler-->>Endpoint: booking id
        Endpoint-->>Client: 201 Created + Location
    end
```

Two checks protect against double booking ([ADR 0004](adr/0004-postgresql-exclusion-constraint.md)):
`BookingService.Reserve` rejects overlaps with the active bookings it is given and returns a precise
`Booking.Overlapping`; the exclusion constraint settles the race when two requests pass that check at the same
time, and the loser gets `Booking.SlotNoLongerAvailable`. Both map to `409 Conflict`.
