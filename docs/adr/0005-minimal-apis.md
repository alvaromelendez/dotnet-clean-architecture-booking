# 0005. Minimal APIs instead of controllers

- Status: Accepted
- Date: 2026-10-16

## Context and problem statement

The HTTP layer is a thin adapter: bind the request, send a command or query, map the `Result` to a response.
It has 13 endpoints and no views, filters or model-binding customizations that would need MVC.

## Decision

Use ASP.NET Core Minimal APIs:

- One class per endpoint implementing `IEndpoint` (`Endpoints/Rooms/*`, `Endpoints/Bookings/*`), discovered by
  reflection (`AddEndpoints`) and mapped onto a versioned group `/api/v{version}` (Asp.Versioning, URL segment).
- Each endpoint declares its OpenAPI metadata (`WithSummary`, `Produces*`), so the native OpenAPI document is
  complete; `OpenApiContractTests` checks it.
- Cross-cutting concerns use the framework: problem details, exception handler, rate limiter, health checks.

## Alternatives considered

- **MVC controllers.** Familiar and feature-rich, but they bring MVC's indirection (filters, conventions,
  model binding pipeline) that none of these endpoints need.
- **A library such as FastEndpoints or Carter.** Similar ergonomics with an extra dependency; the
  `IEndpoint` convention (`IEndpoint` plus `EndpointExtensions`) is under 50 lines.

## Consequences

- Endpoint classes are small and explicit; adding one never touches `Program.cs`.
- Architecture tests enforce that endpoints live only in Api, are named `*Endpoint` and do not reference the
  DbContext.
- Reflection-based discovery is not trimming/AOT friendly; the project does not target Native AOT.
