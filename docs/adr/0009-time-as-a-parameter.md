# 0009. Time as a parameter in Domain; IDateTimeProvider only at the edges

- Status: Accepted
- Date: 2026-10-14

## Context and problem statement

Most rules depend on "now": bookings must start in the future and at most 100 days ahead, refunds depend on
the notice before the start, a booking can only be completed after it ends. Code that reads `DateTime.UtcNow`
directly cannot be tested deterministically.

## Decision

- **Domain never reads the clock.** Every time-dependent operation takes `DateTime utcNow`
  (`BookingService.Reserve`, `Booking.Confirm/Cancel/Complete`, `CancellationPolicy.CalculateRefund`), and
  `DateTimeGuard.EnsureUtc` rejects non-UTC values.
- **Application reads it through `IDateTimeProvider`** (a port) and passes the value in.
- **Infrastructure implements it** (`SystemDateTimeProvider`).
- Architecture tests inspect the IL of Domain and Application and fail if they call the `UtcNow`, `Now` or
  `Today` getters of `DateTime` or `DateTimeOffset`; a positive control proves the rule detects the
  Infrastructure clock (`SystemDateTimeProvider`).

## Alternatives considered

- **`TimeProvider` (.NET 8+) injected into the domain.** Standard and testable, but it is still a dependency of
  domain objects and hides the time inputs of each rule.
- **`IDateTimeProvider` injected into domain services.** Same drawback, plus a port defined in Domain for an
  infrastructure concern.

## Consequences

- Domain tests are pure functions of their inputs; property-based tests can generate "now".
- Every test uses a fixed instant (`TestClock.UtcNow`); no test reads the system clock.
- Call sites are slightly more verbose (`booking.Cancel(dateTimeProvider.UtcNow)`).
