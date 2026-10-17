# Architecture Decision Records

Decisions that shaped BookingSystem, in [MADR](https://adr.github.io/madr/) format: context, decision,
alternatives considered and consequences. Each one describes what the code does today.

| # | Decision | Status |
|---|---|---|
| [0001](0001-clean-architecture.md) | Clean Architecture with dependencies pointing to Domain | Accepted |
| [0002](0002-cqrs-without-event-sourcing.md) | CQRS without Event Sourcing | Accepted |
| [0003](0003-result-pattern.md) | Result pattern for business failures; exceptions only for bugs | Accepted |
| [0004](0004-postgresql-exclusion-constraint.md) | PostgreSQL with an exclusion constraint against double booking | Accepted |
| [0005](0005-minimal-apis.md) | Minimal APIs instead of controllers | Accepted |
| [0006](0006-post-commit-domain-events.md) | Domain events dispatched after commit (at-most-once); Outbox out of scope | Accepted |
| [0007](0007-mediatr-12-for-licensing.md) | Stay on MediatR 12.x for licensing reasons | Accepted |
| [0008](0008-no-authentication-yet.md) | No authentication in v1; path to JWT/OIDC | Accepted |
| [0009](0009-time-as-a-parameter.md) | Time as a parameter in Domain; `IDateTimeProvider` only at the edges | Accepted |
| [0010](0010-testing-strategy.md) | Testing strategy: pyramid, mutation testing and architecture tests | Accepted |

## Adding a record

Copy the most similar record, number it sequentially and keep it to about one page. To change a decision, add a
new record that supersedes the old one and set the old status to `Superseded by NNNN`.
