# Mutation testing notes

Mutation testing runs with [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) over
`BookingSystem.Domain` and `BookingSystem.Application`, using only `BookingSystem.UnitTests`.
Run it with `scripts/mutation.sh` (or `scripts/mutation.ps1`); see [strategy.md](strategy.md) for the thresholds.

## Configuration

Stryker mutates one project per run and the two projects have different `break` thresholds, so each
has its own file: [`stryker-config.domain.json`](../../stryker-config.domain.json) and
[`stryker-config.application.json`](../../stryker-config.application.json). The scripts start Stryker
from `tests/BookingSystem.UnitTests`: started from the repository root, Stryker picks up the solution
and also runs the integration tests, ignoring `test-projects`.

Not mutated (JSON has no comments, so the reasons live here):

| Pattern | Project | Reason |
|---|---|---|
| `**/*Errors.cs` | both | Error catalogs: static data. Tests compare errors by identity (`result.Error.ShouldBe(BookingErrors.X)`), so mutating a code or description string cannot be detected by unit tests. The codes sent to clients are verified on the wire by the integration tests (`ErrorMappingTests`). |
| `**/Events/*DomainEvent.cs` | Domain | Positional records with no behavior. |
| `ErrorType.cs`, `IDomainEvent.cs`, `BookingStatus.cs` | Domain | Enums and a marker interface. |
| `*Dto.cs`, `*Command.cs`, `*Query.cs`, `*Response.cs` | Application | Positional records (requests and results) with no behavior. |
| `Abstractions/{Messaging,Persistence,Clock,Notifications}/*.cs` | Application | Interfaces, the MediatR adapter record and an exception type. |
| `Behaviors/LoggingBehaviorLog.cs` | Application | `[LoggerMessage]` declarations; the bodies are source-generated. |

`ignore-methods: ["*Exception.ctor"]` skips mutations **inside** exception constructor calls, i.e. the
diagnostic message strings. Messages are for developers, not a contract; asserting their exact text would
make tests brittle. Removing the `throw` itself is still mutated and must be killed.

## Results

| Project | Before (Phase 4 tests) | After (Phase 5) | Killed | Survived | No coverage |
|---|---|---|---|---|---|
| Domain | 87.37 % | **98.42 %** | 187 | 1 | 2 |
| Application | 90.52 % | **98.28 %** | 114 | 2 | 0 |

"Before" is the Phase 4 test suite measured with the same configuration. Measured without `ignore-methods`,
the initial Domain score was 81.77 %.

The surviving mutants of the baseline revealed real gaps, now covered by tests:

- `attendees < MinAttendees` → `<=` survived: no test reserved with exactly one attendee
  (`Reserve_WithMinimumAttendees_ReturnsBooking`).
- Guard clauses were never exercised: `ArgumentNullException` and non-UTC checks in `BookingService`,
  `CancellationPolicy`, `Room`, `Money`, `TimeSlot`, `Entity.Raise` and `Result`.
- Clock guards were only tested where a later call would throw anyway; tests now prove the guard runs
  first (`Reserve_WithNonUtcNowAndInactiveRoom_...`, `Cancel_WithNonUtcNowOnTerminalBooking_...`).
- Null checks asserted only the exception type. Without the guard, LINQ's `Any` throws the same type with
  `ParamName = "source"`, so the tests now also assert `ParamName`.
- `Error.None` could carry a code without any test noticing.
- `PagedResult` boundaries (`Page > 1`, `Page < TotalPages`, zero page size), `ValidationError`, the
  not-found path of `ActivateRoom`, the invalid-money and domain-rejection paths of `UpdateRoomDetails`,
  and the blank-name rule of `UpdateRoomDetailsCommandValidator`.

## Equivalent mutants

These survive (or are uncovered) because the mutation does not change observable behavior.

| Location | Mutation | Why it is equivalent |
|---|---|---|
| `Domain/Bookings/Booking.cs:41` | Remove the body of the private parameterless constructor | The body assigns `null!` to auto-properties that are already `null` by default. The constructor only exists for EF Core materialization, so unit tests never call it. |
| `Domain/Rooms/Room.cs:32` | Remove the body of the private parameterless constructor | Same as above. |
| `Domain/Bookings/Booking.cs:82` | Remove `DateTimeGuard.EnsureUtc(utcNow)` in `Booking.Create` | `Create` is `internal` and its only caller, `BookingService.Reserve`, validates the same value first, so this check can never fail. It is kept as defense in depth. |
| `Application/Behaviors/ValidationBehavior.cs:28` | Remove the "no validators" early return | Without validators the rest of the method builds an empty error dictionary and calls `next` anyway. The early return only saves allocations. |
| `Application/Behaviors/ValidationBehavior.cs:62` | `GetGenericArguments().Single()` → `SingleOrDefault()` | This line only runs when `TResponse` is `Result<T>` (plain `Result` returns earlier), which always has exactly one generic argument. |

Mutants with status `CompileError` (7 in Domain, 4 in Application) are mutations the compiler rejects;
Stryker excludes them from the score.
