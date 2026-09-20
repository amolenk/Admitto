# ADR-018: Anonymous shared-scanner bearer-credential boundary

## Status
Accepted.

## Context
Door assistants need to scan attendees at an event without an Admitto account. #95 introduced a per-`TicketedEvent` `ScannerLink` (a high-entropy, recoverable-while-active `ScannerLinkSecret`) managed by team administrators (#97), and #96 made the existing signed-in scanner reusable across access contexts. #98 exposes that link as a working, anonymous check-in path.

This is the first Admitto API surface that authorizes a write (`CheckIn`) without JWT or partner API-key authentication. Every prior anonymous route (`/e/...`) was read-only or a redirect, so no anonymous request had ever reached `AuditInterceptor`.

## Decision
The shared scanner is a public route group (`/scan/{secret}`) outside `/admin`, with no ASP.NET authentication scheme or `[Authorize]` policy. Authorization is a single, narrowly scoped application-level seam instead: `SharedScannerAccess.ResolveActiveEventAsync` resolves the raw secret to its owning `TicketedEvent` and fails closed — one neutral `shared_scanner.access_denied` (401) error — for a malformed secret, no match, an inactive event, or any `ScannerLinkStatus` other than `Active` (expired, revoked, or superseded by regeneration). Every request re-resolves and re-validates; nothing is cached across requests, so an already-open scanner session loses access on its next operation after invalidation.

Once resolved, the shared scanner reuses the existing `CheckInCommand`/`CheckInHandler` and its concurrency reconciliation (`CheckInCommitter`, shared with the admin endpoint) with the resolved team/event ids — the shared-scanner path is a different way to reach the same authoritative check-in, not a parallel implementation. A `CheckInSource` (`Dashboard`/`SharedScanner`) rides along on the command and domain event so the activity-log projection can record the access-context origin as metadata, without inferring or storing any individual door-assistant identity.

Because `AuditInterceptor` always needs a `CreatedBy`/`LastChangedBy` identity and anonymous requests carry no authenticated `HttpContext.User`, `HttpContextUserContextAccessor` now attributes every unauthenticated request's writes to `StaticUserContextAccessor.SystemUser` (previously reserved for background/hosted-service contexts). This is a small, general widening of an existing fallback, not a new mechanism — it applies to any future anonymous write, not only the shared scanner.

## Consequences
- A leaked or brute-forced secret is scoped to exactly one event and stops working the instant it is revoked, regenerated, or the event leaves `Active` — there is no session or token to separately invalidate.
- The failure message is deliberately generic; operators must check the admin scanner-link UI (#97) to learn *why* a given link stopped working.
- Reusing `CheckInCommand`/`CheckInHandler` means registration-level rules (cancelled/wrong-event rejection, one-way attendance, concurrent-scan reconciliation) cannot drift between the signed-in and shared scanners.
- Audit rows for any anonymous write (today, only shared-scanner check-ins) are attributed to the generic system identity; this is acceptable because the goal is operational context, not door-staff accountability.
- Manual name/email lookup is intentionally not part of this credential boundary yet (tracked separately); the shared scanner UI hides that control rather than exposing a non-functional one.

## References
- arc42 §6.6.4 — shared scanner check-in runtime flow.
- arc42 §8 "Shared scanner authorization" — the `SharedScannerAccess` seam and system-user attribution.
- ADR-007 — lifecycle-guard pattern (`TicketedEvent.EnsureActive` gates scanner-link mutations the same way).
