# Security policy

## Supported versions

| Version | Supported |
|---|---|
| 1.0.x | Yes |
| < 1.0 | No |

## Reporting a vulnerability

Please **do not open a public issue** for security problems.

Report them privately through GitHub: **Security → Report a vulnerability** in this repository
(private vulnerability reporting). Include the affected version or commit, steps to reproduce and the impact
you expect. You should get an answer within 7 days.

## Scope and known limitations

This is a portfolio project. Version 1.0 has **no authentication or authorization** by design
([ADR 0008](docs/adr/0008-no-authentication-yet.md)): any caller can act on any booking. Do not expose it to the
internet as is. Reports about the missing authentication are known and out of scope; everything else
(injection, information disclosure in error responses, container hardening, dependency issues) is in scope.

## What CI checks

Every push and pull request runs CodeQL, a gitleaks scan of the full git history and a vulnerable-package check
that fails on High or Critical advisories. Dependabot proposes dependency updates weekly.
