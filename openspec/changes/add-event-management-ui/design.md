## Context

Admitto is a modular monolith where the Organization module owns `TicketedEvent` metadata and the Registrations module owns `EventRegistrationPolicy`, ticket types, and registration flows. Today, admins manage events only through the CLI. We want a first-class tabbed admin UI that mirrors the existing team-settings UX (`settings/layout.tsx` with a side-nav of General / Members / Danger), and we want to stop organizers from accepting registrations before the per-event email configuration has been supplied.

Relevant existing architecture (from `docs/arc42/`):
- Modules communicate only through `*.Contracts` (facades, DTOs, integration events) — §5.2.
- Handlers do not own transactions; endpoints do — §8.1.
- Cross-module scope resolution uses `IOrganizationFacade`/`IOrganizationScopeResolver` — §8.4.
- Use-case slice layout with `AdminApi/` endpoints, validators and handlers — §8.5.
- Email-sending capability runs only in the Worker host (`HostCapability.Email`) — §5.2.1.

Current registration gating is implicit: the Registrations module evaluates a registration window (`EventRegistrationPolicy.SetWindow`) and lifecycle status. There is no explicit "Open for registration" action. The proposal introduces one because the user's mental model — and the required email-configuration guard — is an explicit transition.

## Goals / Non-Goals

**Goals:**
- Tabbed admin UI for per-event settings: **General** (Organization), **Registration** (Registrations), **Email** (Email module).
- New `Admitto.Module.Email` module owning per-event email server settings, with a facade that tells other modules whether email is configured.
- Explicit "Open for registration" / "Close for registration" transitions in the Registrations module, with the Email facade consulted synchronously on open.
- Admin UI can create events, switch tabs, see validation errors, and handle optimistic concurrency per existing patterns.
- CLI parity: every new admin endpoint has a corresponding CLI command.

**Non-Goals:**
- Actually sending email, templating, bounce handling, or reply-to address management beyond the minimum needed to declare email "configured".
- Multi-provider email (we ship with SMTP connection settings as the initial shape; provider abstraction can come later).
- Public-facing registration page changes.
- Replacing the existing window-based gating — opening for registration activates the event; window still controls the accepted time range.

## Decisions

### D1. New Email module vs. extending an existing module
**Decision:** Introduce `Admitto.Module.Email` with its own schema and Contracts project.
**Why:** Email server settings are long-term a distinct concern (templates, providers, bounce processing) and the proposal explicitly flags a future Email module. Starting it now makes the module boundary real instead of retrofitting later. Follows the established pattern of one schema per module (§8.8).
**Alternatives:**
- Put settings in Organization — pollutes team/event aggregates with infra config.
- Put settings in Registrations — couples send-config to a specific consumer; future Email work (templates, outbox worker) belongs elsewhere.

### D2. Cross-module check uses a synchronous facade call
**Decision:** Registrations' `OpenRegistration` command calls `IEventEmailFacade.IsEmailConfiguredAsync(eventId)` from its handler. If false, the handler throws a `BusinessRuleViolationException` using a handler-local error (§8.7 tier 3).
**Why:** This is a precondition check, not a cross-aggregate transaction. The Email module is in-process and the call is a cheap read. Synchronous guards are consistent with how Registrations already depends on `IOrganizationFacade` for lookups. An async/event-driven approach would force the user to wait for eventual confirmation that the event is open, which is bad UX for an admin action.
**Alternatives:**
- Integration event published by Email when configured, consumed by Registrations to flip a local flag → extra eventual-consistency surface for no benefit.
- Frontend-only check → bypassable, violates "defence in depth".
- Still add a frontend pre-check for UX: disable the "Open" button and show a hint when email is not configured (belt-and-braces; backend remains the source of truth).

### D3. "Open for registration" as an explicit state
**Decision:** Add `RegistrationStatus { Draft, Open, Closed }` (or equivalent) on `EventRegistrationPolicy`, with explicit `OpenForRegistration()` / `CloseForRegistration()` methods. Self-service and coupon flows require `Open` (and existing window/lifecycle rules still apply).
**Why:** The email guard needs a single clear transition to attach to. Also matches the proposed UI (an explicit "Open" button on the Registration tab).
**Alternatives considered:**
- Treat "setting the registration window" as opening → too implicit; no natural place to surface the email error.
- Store "opened" as a lifecycle status on `TicketedEvent` in Organization → crosses the module boundary (registration gating is owned by Registrations per §8.6 lifecycle-sync rationale).

### D4. Module boundaries for the UI
**Decision:** The UI calls separate backend endpoints per tab:
- **General tab** → Organization endpoints (`PUT /admin/teams/{teamSlug}/events/{eventSlug}`).
- **Registration tab** → Registrations endpoints for window, domain restriction, ticket types, and open/close.
- **Email tab** → Email module endpoints (`GET|PUT /admin/teams/{teamSlug}/events/{eventSlug}/email-settings`).
Each tab page loads only the data it owns. No aggregated "event" endpoint that pierces modules.
**Why:** Preserves module boundaries (§5.2), keeps each endpoint aligned with one module's unit-of-work (§8.1), and avoids the coordinating-transaction anti-pattern. Tabs are natural boundaries — users edit one concern at a time.
**Alternatives:** A composite backend DTO aggregating all three modules — violates boundaries and complicates concurrency (three `Version` tokens in one payload).

### D5a. Protecting email credentials at rest
**Decision:** Use the **ASP.NET Core Data Protection API** to encrypt the email connection string (and any other secret fields such as SMTP password or API tokens) before persisting to the `email.event_email_settings` table. The encrypted blob is stored as a `text`/`bytea` column; decryption happens on read inside the Email module's infrastructure layer (e.g. via an EF value converter or a dedicated `IProtectedSecret` adapter).
**Why:** Connection strings carry credentials that grant outbound mail access on behalf of the team. Storing them in plaintext would violate basic at-rest protection expectations and make a database leak directly exploitable. Data Protection API is already in the .NET stack, integrates with key-ring storage suitable for a multi-host deployment, and supports key rotation without re-encrypting historical rows in bulk (old keys remain usable for decryption).
**Implementation notes:**
- Configure a single shared key ring (e.g. persisted to a database table or blob storage) so the API host writes settings that the Worker host can later decrypt for sending.
- Use a dedicated purpose string (e.g. `"Admitto.Email.ConnectionString.v1"`) so other features cannot decrypt these blobs by accident.
- Never log decrypted secrets; mask the connection string in any admin GET response (return only metadata such as host/port/from-address; require re-entry to update).
**Alternatives:**
- Plaintext column → unacceptable.
- Cloud-provider KMS (Azure Key Vault) → heavier setup, environment-specific, deferrable; Data Protection's KeyVault-backed key ring can be added later without changing the storage shape.

### D5. Email "configured" definition
**Decision:** Email is "configured" when an `EventEmailSettings` row exists for the event with the minimum viable fields populated: SMTP host, port, from-address, and either credentials or an explicit "no-auth" flag. The facade returns true iff such a row exists and its `IsValid` domain check passes. No SMTP connectivity probe is performed.
**Why:** A connectivity probe is slow, flaky, and out of scope for a pre-commit guard. The UI can offer an explicit "Send test email" action later.
**Alternatives:** Require a successful test send — rejected as too invasive for a gate on `OpenForRegistration`.

### D6. Optimistic concurrency and versions
**Decision:** Every aggregate (`TicketedEvent`, `EventRegistrationPolicy`, `EventEmailSettings`) carries its own `Version` (§8.8). Tab pages fetch + mutate with their own token. Opening registration is a mutation of `EventRegistrationPolicy` only; the email check is read-only against the Email module, so no cross-aggregate write coordination is needed.
**Why:** Keeps each tab's edits independently resubmittable on conflict. Matches existing pattern for teams.

### D7. Capability gating for Email module
**Decision:** The Email module's admin-setting endpoints and facade register in **any** host (no `RequiresCapability`). Only actual sending (future) requires `HostCapability.Email`.
**Why:** The facade and settings CRUD are metadata; they must be available wherever admin endpoints run (API host). This matches §5.2.1.

### D8. UI layout
**Decision:** Route structure `teams/[teamSlug]/events/[eventSlug]/settings/{general|registration|email}` with a shared `layout.tsx` side-nav (same pattern as `teams/[teamSlug]/settings`). "Open/Close registration" is a primary action on the Registration tab, disabled with tooltip when Email tab reports not configured (read via a lightweight status query).

### D9. Org→Registrations event-creation sync (corrected during implementation)
**Decision:** When the Organization module creates a `TicketedEvent`, it raises `TicketedEventCreatedDomainEvent` (carrying both `TeamId` and `TicketedEventId`). `OrganizationMessagePolicy` publishes a corresponding `TicketedEventCreatedModuleEvent` from `Admitto.Module.Organization.Contracts`. The Registrations module consumes that event via `TicketedEventCreatedModuleEventHandler`, which dispatches `HandleEventCreatedCommand` through the mediator. The handler is **idempotent**: if a policy with the same `TicketedEventId` already exists it is a no-op; otherwise it creates an `EventRegistrationPolicy` with `EventLifecycleStatus = Active` and `RegistrationStatus = Draft`.

Every Registrations command/query handler that requires the policy (`OpenRegistration`, `CloseRegistration`, `SetRegistrationPolicy`, `AddTicketType`, `UpdateTicketType`, `CancelTicketType`, `CreateCoupon`, `SelfRegisterAttendee`, `RegisterWithCoupon`, `GetRegistrationOpenStatus`) looks up the policy by id and throws `EventRegistrationPolicy.Errors.EventNotFound` (`ErrorType.NotFound` → HTTP 404) when it is missing. **No handler creates a policy on demand.**

**Why:** The previous implementation conflated "this event doesn't exist in Registrations" with "this event is not active": several handlers either auto-created the policy or returned an `EventNotActive` error when the row was missing. This made event-creation-sync bugs invisible (everything kept working by accident) and produced misleading error messages when an unknown event id was supplied. Trusting the eventing pipeline for creation gives:
- a single, explicit owner for the `Draft + Active` initialization;
- a clear, distinct `NotFound` error for genuinely-unknown events;
- handlers that read more like the registration-policy aggregate's actual contract.

**Alternatives:**
- Keep auto-create branches as a safety net → hides sync bugs, conflates two errors, and forces every handler to know default-policy state.
- Backfill missing rows lazily on the Organization side → adds cross-module write coordination for a problem that the existing module-event mechanism already solves.

**Notes:**
- Cancel/Archive sync handlers retain their defensive auto-create branches (`HandleEventCancelled`, `HandleEventArchived`) because they are passive sync, not user actions, and the resulting `Cancelled`/`Archived` policy is harmless if an out-of-order Created event later arrives — which is an extremely unlikely path now that Created is also synced.
- The mediator wraps `HandleEventCreatedCommand` with a `DeterministicCommandId` derived from the module event's id, so retries land on the same idempotency key.

## Risks / Trade-offs

- **Risk:** Introducing a new module increases surface area (schema, DbContext, migration host wiring, module-event registration, etc.).
  **Mitigation:** Keep the initial Email module minimal — one aggregate, CRUD endpoints, one facade method. Reuse shared infrastructure (`AddModuleDatabaseServices`, auto-registration of handlers).

- **Risk:** Adding a new `RegistrationStatus` is a schema/behavior change on an aggregate already in use.
  **Mitigation:** Default existing rows to `Draft` in the migration; evaluate whether existing integration tests/seed data need updating. Document in tasks.

- **Risk:** Synchronous cross-module facade call creates a direct dependency from Registrations on Email.
  **Mitigation:** Dependency goes only through `Admitto.Module.Email.Contracts`, following the established Organization→Registrations pattern. No cross-module DbContext access.

- **Risk:** UI complexity with three tabs, three versions, three endpoints per event.
  **Mitigation:** Each tab is an independent page (not a single mega-form), so React Hook Form state stays local. TanStack Query handles refresh on tab switch.

- **Risk:** Tests may need updating for existing Registrations flows if we add the `RegistrationStatus` guard retroactively.
  **Mitigation:** Decide in tasks whether existing self-service flow tests should pre-set status to `Open`, or whether current behavior (no explicit open) is treated as `Open` by default for already-seeded rows.

## Migration Plan

1. Create Email module projects and wire into `Admitto.Api`, `Admitto.Worker`, `Admitto.Migrations`.
2. Add EF migration for `email.event_email_settings`; apply via Migrations host per `ef-migrations` skill.
3. Add `RegistrationStatus` column to `registrations.event_registration_policies` with default `Open` for existing rows (so we don't retroactively close live events) but `Draft` for newly created ones going forward.
4. Ship backend endpoints + CLI commands first; then UI.
5. Rollback: disable new UI routes; revert RegistrationStatus column to non-nullable default-Open; Email schema can be left in place (no data loss risk).

## Open Questions

- **Q1:** Should newly created events default to `Draft` (explicit open required) or `Open` (backwards-compatible)? Recommendation: `Draft` for new events, `Open` for rows migrated from pre-existing data — see migration step 3.
- **Q2:** Email settings granularity — per event only, or inheritable from team? Current decision: per event only, matching the user's description. Team-level defaults can be added later without breaking the facade contract.
- **Q3:** Should the "Email configured" status be surfaced on the Registration tab via a Registrations endpoint (which would then call the Email facade), or by the UI calling the Email endpoint directly? Recommendation: Registrations exposes a tiny "can open?" status query so the UI doesn't cross module APIs for a business rule.
- **Q4:** ADR needed for the cross-module synchronous facade pattern, or is it sufficiently covered by existing `IOrganizationFacade` precedent? Recommendation: no new ADR; mention in `docs/arc42/05-building-block-view.md` and §8.4 update.
