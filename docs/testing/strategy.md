# Test strategy

The suite is split by what each test needs to run, from fast and isolated to slow and realistic.
Each test project is one category, applied with an assembly-level trait (`[assembly: AssemblyTrait("Category", ...)]`),
so new tests are categorized automatically.

```
                ┌────────────────────────┐
                │   Integration (86)     │  PostgreSQL (Testcontainers) + in-memory API host
            ┌───┴────────────────────────┴───┐
            │          Unit (316)            │  Domain + Application, in memory, milliseconds
            └────────────────────────────────┘
   Architecture (39): structural rules over the compiled assemblies, alongside every level
```

| Level | Project | Covers | Needs Docker |
|---|---|---|---|
| Unit | `BookingSystem.UnitTests` | Domain rules and state machines, value objects, the cancellation policy, handlers (with NSubstitute fakes for ports), validators and pipeline behaviors. Includes 8 property-based tests (FsCheck) for `TimeSlot.OverlapsWith`, `Money` and `CancellationPolicy`. | No |
| Integration | `BookingSystem.IntegrationTests` | EF Core mappings, migrations, the exclusion constraint and optimistic concurrency against a real PostgreSQL; the HTTP API end to end (status codes, problem details, versioning, health checks) and the OpenAPI contract. | Yes |
| Architecture | `BookingSystem.ArchitectureTests` | Layer dependency rule, naming and visibility conventions, no system clock in Domain/Application, Domain project without references. Built with ArchUnitNET. | No |

## Running the tests

| Command | What runs |
|---|---|
| `dotnet test` | Everything (Docker must be running) |
| `dotnet test --filter "Category=Unit"` | Unit tests only |
| `dotnet test --filter "Category=Integration"` | Integration tests only |
| `dotnet test --filter "Category=Architecture"` | Architecture tests only |
| `dotnet test --filter "Category!=Integration"` | The fast suite, no Docker needed |
| `scripts/coverage.sh` / `scripts/coverage.ps1` | All tests with coverage, report and thresholds |
| `scripts/mutation.sh` / `scripts/mutation.ps1` | Stryker.NET over Domain and Application |

## Rules every test follows

- **No system clock.** "Now" is always `TestClock.UtcNow` (2026-03-02 09:00 UTC): handlers get a fake
  `IDateTimeProvider`, the API host gets a frozen one, and builders and generators are anchored to it.
- **No ordering assumptions and no shared mutable state.** Integration tests share one PostgreSQL container
  (started once per run) but never clean it: each test creates its own rooms (unique names) and users, and
  only asserts on that data. This was verified by running every integration test class in isolation and the
  whole suite in three random orders.
- **No `Thread.Sleep`** or timing-based waits.
- **Valid test data through the domain.** `RoomBuilder`, `BookingBuilder` and `TimeSlotBuilder` live in
  `BookingSystem.TestUtilities` (a plain class library shared by both test projects, with no test-framework
  packages). `BookingBuilder` reaches every status through `BookingService.Reserve` and the public
  transitions, never through reflection, so tests cannot depend on states the domain would not produce.

## Coverage

`coverlet.collector` gathers coverage with the settings in [`coverlet.runsettings`](../../coverlet.runsettings).
Each exclusion there is justified in a comment: migrations, `Program`, the design-time DbContext factory,
compiler- and source-generator output, and pure data records (DTOs, commands, queries, API contracts).
ReportGenerator (pinned in `.config/dotnet-tools.json`) merges the reports of all test projects into
`coverage-report/` (HTML, Cobertura XML and a Markdown summary).

`scripts/coverage.sh` fails when line coverage is below these thresholds, declared once at the top of the script:

| Scope | Threshold | Before | After | Why this threshold |
|---|---|---|---|---|
| Domain | 95 % | 97.17 % | 100 % | Pure, deterministic code that holds the business rules; it is cheap to test thoroughly. The margin below 100 % avoids pressure to write assertion-free tests for trivial members. |
| Application | 90 % | 97.43 % | 99.14 % | Thin orchestration over the domain and ports, tested with fakes. Boilerplate such as exception constructors does not need dedicated tests. |
| Global | 85 % | 94.93 % | 96.13 % | Api and Infrastructure are mostly covered by the slower integration tests, and some framework glue (for example the rate-limit rejection callback) is costly to exercise for little value. |

"Before" is the Phase 4 suite measured with the same settings.

## Mutation testing

Coverage only proves that code ran; mutation testing proves that assertions would notice if it changed.
Stryker.NET mutates Domain and Application and runs **only the unit tests**: the integration tests would
multiply the run time and need Docker. Configuration, excluded files and equivalent mutants are documented
in [mutation-notes.md](mutation-notes.md).

| Project | high | low | break | Before | After |
|---|---|---|---|---|---|
| Domain | 90 | 80 | 70 | 87.37 % | 98.42 % |
| Application | 90 | 80 | 60 | 90.52 % | 98.28 % |

The Domain `break` is higher because every surviving mutant there is a business rule nobody checks.
Application handlers mostly delegate to the domain and to faked ports, where some mutants are only
observable through the integration tests that Stryker does not run, so its floor is lower.

## Sabotage check

Three bugs were introduced by hand, one at a time, and `dotnet test --filter "Category=Unit"` caught each one:

| Bug | Detected by |
|---|---|
| `TimeSlot.OverlapsWith`: `Start < other.End` → `<=` | 5 tests: two adjacency cases of `TimeSlotTests.OverlapsWith_OtherSlot_ReturnsExpectedResultInBothDirections` and the symmetry, adjacency and definition properties |
| `CancellationPolicy`: full-refund notice 24 h → 12 h | `CancellationPolicyTests.CalculateRefund_ConfirmedBooking_ReturnsRefundBasedOnNotice` (23:59:59 notice → 50 %) |
| `BookingService.Reserve`: capacity check removed | `BookingServiceTests.Reserve_WithAttendeesAboveCapacity_ReturnsCapacityExceededError` and `BuilderTests.BookingBuilder_WhenDomainRejectsReservation_ThrowsNamingTheDomainError` |
