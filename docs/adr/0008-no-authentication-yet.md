# 0008. No authentication in v1 (userId in the request); path to JWT/OIDC

- Status: Accepted
- Date: 2026-10-16

## Context and problem statement

Bookings belong to users, but identity management is orthogonal to the architecture this project
demonstrates. Implementing it half-way (for example, homemade API keys) would add risk without value.

## Decision

Version 1 has **no authentication or authorization**:

- `POST /api/v1/bookings` takes `userId` in the body (`CreateBookingRequest`); `GET /api/v1/users/{userId}/bookings`
  takes it in the route.
- Any caller can act on any booking. This is stated in the README under "Known limitations" and in
  `SECURITY.md`; the API must not be exposed publicly as is.

Path to production-grade auth, without changing Domain:

1. Add JWT bearer authentication against an OIDC provider (Entra ID, Auth0, Keycloak) in Api.
2. Take the user id from the `sub` claim through an `ICurrentUser` port implemented in Api; remove `userId` from
   request contracts (a v2 API version if v1 clients must keep working).
3. Add ownership checks in the handlers (a booking can only be confirmed or cancelled by its owner or an admin)
   and authorization policies for room management.
4. Partition the rate limiter by user instead of by IP.

## Alternatives considered

- **ASP.NET Core Identity with local accounts.** Brings user storage, password policies and email flows that are
  out of scope.
- **API keys.** Simple, but not per-user identity and easy to get wrong.

## Consequences

- The demo is easy to run and test with curl.
- Not safe for real users until the steps above are done.
