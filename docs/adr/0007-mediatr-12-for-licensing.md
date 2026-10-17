# 0007. Stay on MediatR 12.x for licensing reasons

- Status: Accepted
- Date: 2026-10-14 (license status re-checked 2026-10-16)

## Context and problem statement

The application layer needs in-process dispatch of commands, queries and notifications, plus pipeline
behaviors (logging, validation). MediatR is the de facto library for this, but its license changed.

Verified on nuget.org (package metadata):

- `MediatR` **12.5.0**: `Apache-2.0`.
- `MediatR` **13.0.0 and later** (latest 14.2.0): a custom `LICENSE.md`. The author announced that from 13.0
  MediatR is dual-licensed under RPL-1.5 or a commercial license from Lucky Penny Software, which requires a
  license key; a free Community tier covers organizations under a revenue threshold, education and
  non-production use ([announcement](https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/)).

## Decision

Pin `MediatR` to **12.x** (Apache-2.0). Dependabot ignores its major updates (`.github/dependabot.yml`).

MediatR is confined to Application (handlers, behaviors) and the Api endpoints (`ISender`), and Domain does not
depend on it, so replacing it later touches a known, small surface.

## Alternatives considered

- **MediatR 13+/14 with a Community license.** Free for this project today, but it adds a license key and terms
  that anyone forking a portfolio project would have to evaluate.
- **A source-generated mediator** (e.g. `Mediator` by martinothamar) or **a hand-written dispatcher**.
  Viable; postponed because 12.x is stable and fully functional.
- **No mediator** (handlers injected directly into endpoints). Loses the pipeline behaviors.

## Consequences

- No license key or commercial terms; the 12.x line will not receive new features and only receives fixes, if
  any, at the maintainer's discretion. Vulnerability scanning in CI would flag a security issue.
- Migrating away is a planned roadmap item, not an emergency.
