# 0006. Domain events dispatched after commit (at-most-once); Outbox out of scope

- Status: Accepted
- Date: 2026-10-15

## Context and problem statement

Aggregates raise domain events (`BookingCreated`, `BookingConfirmed`, `BookingCancelled`, `BookingCompleted`,
`RoomDeactivated`). Handlers (today, notifications through a logging stub) must not run for changes that were
never committed, and the domain must not depend on a messaging library.

## Decision

`UnitOfWork.SaveChangesAsync`:

1. Collects and clears the events of tracked aggregates.
2. Commits.
3. Only then publishes each event through MediatR, wrapped in `DomainEventNotification<T>` (an Application type,
   so Domain stays free of MediatR). A failing handler is logged and does not fail the request, because the
   data is already committed.

Delivery is therefore **at-most-once**: a crash between the commit and the publish loses the events.

## Alternatives considered

- **Dispatch before commit** (inside the transaction). Handlers could react to changes that are later rolled
  back, and slow handlers would hold the transaction open.
- **Transactional Outbox.** Store events in the same transaction and publish them from a background worker:
  at-least-once delivery, but it needs an outbox table, a worker, retries and idempotent handlers. It is the
  right next step if a handler ever performs an action that must not be lost (email, payment, integration).
- **A message broker.** Out of scope for a single service with no consumers.

## Consequences

- Simple and correct for best-effort side effects such as notifications.
- Events can be lost; this is documented as a known limitation, and the code comment in `UnitOfWork` points to
  the Outbox as the fix.
