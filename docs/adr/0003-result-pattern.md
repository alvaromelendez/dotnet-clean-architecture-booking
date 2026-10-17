# 0003. Result pattern for business failures; exceptions only for bugs

- Status: Accepted
- Date: 2026-10-14

## Context and problem statement

Many failures are expected outcomes of a valid request: the room is inactive, the slot overlaps, the booking is
not pending. Throwing exceptions for them hides the failure modes from method signatures, makes control flow
implicit and mixes them with genuine bugs in logs.

## Decision

- Domain and application operations return `Result` / `Result<T>` (`BookingSystem.Domain.Abstractions`). A
  failure carries an `Error(Code, Description, Type)` with a stable code such as `Booking.Overlapping` and an
  `ErrorType` (`Validation`, `NotFound`, `Conflict`, `Failure`).
- `ValidationBehavior` returns a failed result with a `ValidationError` instead of throwing.
- The API maps `ErrorType` to HTTP status codes in one place (`ResultExtensions.ToProblem`): RFC 9457
  problem details with `code` and `traceId`.
- Exceptions are reserved for programming errors and broken invariants: null arguments, non-UTC timestamps
  (`DateTimeGuard`), reading `Value` of a failed result. `GlobalExceptionHandler` turns them into a 500 without
  leaking details.
- One infrastructure exception crosses into Application on purpose: `ConcurrencyConflictException`, which the
  handler that can recover from it (`CreateBooking`) converts back into a `Result`.

## Alternatives considered

- **Exceptions for everything** with a middleware mapping them to status codes. Less code, but failure modes
  are invisible to callers and expensive on hot paths.
- **A library (FluentResults, ErrorOr, OneOf).** Viable; the hand-written type is about 100 lines and keeps the
  Domain project free of package references ([0001](0001-clean-architecture.md)).

## Consequences

- Every handler signature documents that it can fail, and tests assert on error identity.
- Some boilerplate (`if (result.IsFailure) return result.Error;`), since C# has no `?`-style propagation.
- Error codes are part of the public contract; `ErrorMappingTests` verifies them on the wire.
