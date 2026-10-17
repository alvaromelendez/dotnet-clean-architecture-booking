# 0010. Testing strategy: pyramid, mutation testing and architecture tests

- Status: Accepted
- Date: 2026-10-16

## Context and problem statement

The value of the project is in its rules and its boundaries. Tests must prove the rules, prove the database
protections against real PostgreSQL, keep the architecture from eroding and stay fast enough to run on every
push.

## Decision

Three test projects, one category each (assembly-level `Category` trait):

| Category | Project | What it proves |
|---|---|---|
| Unit | `BookingSystem.UnitTests` | Domain rules and state machine, value objects, handlers with NSubstitute fakes, validators, behaviors; FsCheck property-based tests for `TimeSlot`, `Money` and `CancellationPolicy`. |
| Integration | `BookingSystem.IntegrationTests` | EF Core mappings, migrations, the exclusion constraint and optimistic concurrency on PostgreSQL (Testcontainers); the HTTP API end to end with `WebApplicationFactory`, including the OpenAPI contract. |
| Architecture | `BookingSystem.ArchitectureTests` | Layer dependencies, naming and visibility conventions, no system clock in Domain/Application (ArchUnitNET). |

Quality gates, each defined in one place:

- **Coverage** with coverlet + ReportGenerator: `scripts/coverage.sh` fails below 95 % (Domain), 90 %
  (Application) or 85 % (global) line coverage.
- **Mutation testing** with Stryker.NET on Domain and Application (unit tests only): `break` thresholds in
  `stryker-config.*.json` (70 and 60). Weekly in CI, never on pull requests, because a run takes minutes.

Rules: no system clock, no ordering assumptions, no shared mutable state, no sleeps; test data built through the
domain (`BookingSystem.TestUtilities` builders). Details: [docs/testing/strategy.md](../testing/strategy.md).

## Alternatives considered

- **EF Core in-memory or SQLite for integration tests.** Faster, but cannot run the exclusion constraint,
  `tstzrange` or `xmin` concurrency: the most important protections would be untested.
- **Coverage only.** Coverage proves code ran, not that assertions would catch a change; mutation testing does.
- **NetArchTest.** No release since 2021; ArchUnitNET is maintained.

## Consequences

- Integration tests need Docker (locally and in CI; GitHub's Ubuntu runners provide it).
- Two quality signals (coverage and mutation score) with documented thresholds instead of a vanity 100 %.
