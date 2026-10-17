# 0001. Clean Architecture with dependencies pointing to Domain

- Status: Accepted
- Date: 2026-10-14

## Context and problem statement

The business rules of a reservation system (availability, pricing, the booking lifecycle, refunds) change
for business reasons, while the database, the web framework and the messaging library change for technical
ones. Mixing them makes the rules hard to test and every framework upgrade risky.

## Decision

Four projects with an inward-only dependency rule:

```
Api ──► Infrastructure ──► Application ──► Domain
 └────────────────────────►┘
```

- **Domain** holds entities, value objects, domain services and errors. `BookingSystem.Domain.csproj` has
  no project or package references.
- **Application** holds use cases and the ports they need (`IBookingRepository`, `IUnitOfWork`,
  `IDateTimeProvider`, `INotificationSender`, ...).
- **Infrastructure** implements the ports with EF Core/PostgreSQL.
- **Api** is the HTTP adapter and the composition root.

The rule is enforced, not just documented: `BookingSystem.ArchitectureTests` (ArchUnitNET) checks the layer
dependencies, that Domain references only the base class library and that its project file declares no
references.

## Alternatives considered

- **Single project with folders.** Less ceremony, but nothing stops a controller from using the DbContext or
  the domain from referencing EF Core; boundaries erode silently.
- **Vertical slices only.** Excellent for feature cohesion, but less suited to showcasing a rich, framework-free
  domain model. Application use cases *are* organized as vertical slices inside the layer.

## Consequences

- Domain and Application are tested in memory, in milliseconds (316 unit tests).
- More projects and mapping code (domain → DTO → API contract) than a minimal API over EF Core would need.
- Domain types carry private parameterless constructors for EF Core materialization: a small, accepted leak.
