# 0004. PostgreSQL with an exclusion constraint against double booking

- Status: Accepted
- Date: 2026-10-15

## Context and problem statement

Two bookings of the same room must never overlap while active (`Pending` or `Confirmed`). `BookingService.Reserve`
checks overlaps against the active bookings it is given, but that check alone cannot be enough: two concurrent
requests can both read "free", both pass the check and both insert. Classic check-then-act race.

## Decision

Use PostgreSQL and make the database the final arbiter with a GiST **exclusion constraint** (initial migration):

```sql
ALTER TABLE bookings ADD CONSTRAINT ex_bookings_room_id_time_slot
  EXCLUDE USING gist (room_id WITH =, tstzrange(start_utc, end_utc, '[)') WITH &&)
  WHERE (status IN ('Pending', 'Confirmed'));
```

- `btree_gist` combines uuid equality with range overlap. The `[)` bounds mirror the domain's half-open
  `TimeSlot`, so back-to-back bookings are allowed; cancelled and completed bookings do not block.
- `UnitOfWork` translates SQLSTATE `23P01` into `ConcurrencyConflictException`; `CreateBookingCommandHandler`
  returns `Booking.SlotNoLongerAvailable` (409).
- The in-memory check stays: it gives a precise error (`Booking.Overlapping`) on the common path without
  relying on a database error.

## Alternatives considered

- **Serializable isolation.** Correct, but every booking transaction may need retries and the protection
  depends on every writer using the right isolation level.
- **Pessimistic lock** (`SELECT ... FOR UPDATE` on the room). Serializes all bookings of a room and is easy to
  forget in a new code path.
- **Application-level lock or a unique index on fixed slots.** Does not work across instances, or forces a
  fixed slot grid.

## Consequences

- Correct under concurrency regardless of the code path that writes; proven by
  `ConcurrencyTests.CreateBooking_TwoOverlappingRequestsInParallel_ExactlyOneSucceeds` and `UnitOfWorkTests`.
- Ties the project to PostgreSQL (the constraint is raw SQL in the migration; EF Core cannot model it).
- Integration tests must run against real PostgreSQL (Testcontainers), not an in-memory provider.
