# 0002. CQRS without Event Sourcing

- Status: Accepted
- Date: 2026-10-14

## Context and problem statement

Writes must enforce invariants (capacity, overlaps, state transitions) and need whole aggregates. Reads
(list rooms, available rooms, a user's bookings) only need flat data, often filtered and paginated, and
loading aggregates for them wastes memory and round trips.

## Decision

Separate the two paths at the application level, over a **single PostgreSQL database**:

- **Commands** (`ICommand`, `ICommand<T>`) load aggregates through repositories, call domain methods and
  commit through `IUnitOfWork`.
- **Queries** (`IQuery<T>`) go through read-side ports (`IRoomQueries`, `IBookingQueries`) whose
  implementations use `AsNoTracking` and project straight to DTOs in SQL (for example, availability is a
  `NOT EXISTS` subquery; user bookings are paginated with a stable `created_at_utc DESC, id` order).
- Both are dispatched with MediatR (see [0007](0007-mediatr-12-for-licensing.md)); state is stored as current
  rows, not as an event stream.

## Alternatives considered

- **Event Sourcing.** Full history and temporal queries, but projections, versioning and replay add a lot of
  complexity that no requirement here asks for.
- **Separate read database.** Scales reads independently but introduces eventual consistency between the
  write and read models with no measured need.
- **Repositories for everything.** Simpler, but queries would load aggregates and map in memory.

## Consequences

- Read queries are cheap and shaped for the API; write handlers stay small.
- Reads are strongly consistent with writes (same database and transaction log).
- Two models of the same tables (aggregates and DTO projections) must be kept in sync by hand; the integration
  tests cover the projections against a real PostgreSQL.
