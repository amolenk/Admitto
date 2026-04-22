## 1. Setup & shared scaffolding

- [x] 1.1 Add new shared integration-event base records under `src/Admitto.Module.Shared/Application/Messaging/IntegrationEvents/`: `TicketedEventCreationRequested`, `TicketedEventCreated`, `TicketedEventCreationRejected`, `TicketedEventCancelled`, `TicketedEventArchived` (each carrying at minimum `TeamId`, `TicketedEventId` (where applicable), `Slug` (where applicable), and `CreationRequestId` for create flows).
- [x] 1.2 Add a `CreationRequestId` strongly-typed id under the Organization module's `Domain/ValueObjects/`.
- [x] 1.3 Add `TicketedEventId` strongly-typed id under the Registrations module (move/rename from Organization if it currently lives there).
- [x] 1.4 Confirm outbox + Azure Storage Queue wiring already routes the new integration events (no new transport work); add subscription registrations in each module's `DependencyInjection.cs`.

## 2. Organization module — Team aggregate redesign

- [x] 2.1 Add `ActiveEventCount`, `CancelledEventCount`, `ArchivedEventCount`, `PendingEventCount` (non-negative ints, default 0) to the `Team` aggregate with guarded mutators.
- [x] 2.2 Add `TeamEventCreationRequest` child entity under `Team` (`CreationRequestId`, `RequestedSlug`, `RequesterId`, `RequestedAt`, `Status` enum {Pending, Created, Rejected, Expired}, `TicketedEventId?`, `RejectionReason?`).
- [x] 2.3 Update `Team.Archive()` invariant to require `ActiveEventCount == 0 && PendingEventCount == 0` (replace any existing event-list check).
- [x] 2.4 Remove the legacy `TicketedEvent` aggregate, EF configuration, and EF DbSet from Organization (`Domain/Entities/TicketedEvent.cs` and related repository code).
- [x] 2.5 Generate the EF Core migration in `Admitto.Migrations` for Organization: drop legacy event/policy tables, add counter columns to `Team`, add `TeamEventCreationRequest` table.
- [x] 2.6 Domain unit tests for counter invariants (no negative values), archive guard with active/pending counts, and creation-request lifecycle transitions.

## 3. Organization module — event creation gatekeeper

- [x] 3.1 Implement `RequestTicketedEventCreationCommand` + handler that validates team is not archived, increments `PendingEventCount`, persists a `TeamEventCreationRequest` in `Pending`, and outboxes `TicketedEventCreationRequested` (all in one UoW).
- [x] 3.2 Add endpoint `POST /admin/teams/{teamSlug}/events` returning `202 Accepted` with `Location: /admin/teams/{teamSlug}/event-creations/{creationRequestId}`.
- [x] 3.3 Add endpoint `GET /admin/teams/{teamSlug}/event-creations/{creationRequestId}` returning status, slug, timestamps, optional rejection reason, and (when `Created`) a link to the new event.
- [x] 3.4 Implement integration-event handlers in Organization for `TicketedEventCreated` (mark request `Created`, decrement pending, increment active) and `TicketedEventCreationRejected` (mark `Rejected`, decrement pending, store reason). Idempotent on `CreationRequestId`.
- [x] 3.5 Implement integration-event handlers in Organization for `TicketedEventCancelled` (active→cancelled counter swap) and `TicketedEventArchived` (decrement source counter, increment archived). Idempotent on `TicketedEventId` + observed transition.
- [x] 3.6 Implement Quartz job `ExpireStaleEventCreationRequestsJob` (configurable timeout, default 24h) that transitions `Pending` requests to `Expired` and decrements `PendingEventCount`. Register schedule in worker host.
- [x] 3.7 Application/integration tests covering: accept on active team, reject on archived team, idempotent redelivery of each integration event, expiry job behaviour, archive blocked by pending count.

## 4. Registrations module — TicketedEvent aggregate

- [x] 4.1 Create `TicketedEvent` aggregate under `src/Admitto.Module.Registrations/Domain/Entities/` with `Id`, `TeamId`, `Slug`, `Name`, `StartsAt`, `EndsAt`, `Status` (Active/Cancelled/Archived), `Version`, and three policy value objects.
- [x] 4.2 Create value objects `TicketedEventRegistrationPolicy` (window + optional email-domain restriction), `TicketedEventCancellationPolicy` (optional cutoff datetime), `TicketedEventReconfirmPolicy` (window + cadence days, all optional).
- [x] 4.3 Implement aggregate methods: `Cancel()`, `Archive()`, `UpdateDetails()`, `ConfigureRegistrationPolicy(...)`, `ConfigureCancellationPolicy(...)`, `ConfigureReconfirmPolicy(...)`. Each policy mutator rejects when status is not Active.
- [x] 4.4 Implement domain events `TicketedEventStatusChanged` (Active→Cancelled, *→Archived) raised by `Cancel()` / `Archive()`.
- [x] 4.5 EF configuration: `(TeamId, Slug)` unique index; rowversion concurrency token.
- [x] 4.6 Delete `TicketedEventLifecycleGuard` aggregate, its repository, EF config, and all references.
- [x] 4.7 Delete standalone `EventRegistrationPolicy`, `EventCancellationPolicy`, `EventReconfirmPolicy` aggregates and their EF configurations.
- [x] 4.8 Domain unit tests: status transitions (legal + illegal), policy mutation rejection in Cancelled/Archived, policy value-object validation (close-after-open, positive cadence, etc.).

## 5. Registrations module — TicketCatalog extension

- [x] 5.1 Add `EventStatus` field (Active/Cancelled/Archived, default Active on creation) to `TicketCatalog`.
- [x] 5.2 Add status-transition methods on `TicketCatalog` enforcing one-way moves (Active→Cancelled, Active→Archived, Cancelled→Archived) and rejecting illegal transitions.
- [x] 5.3 Update `TicketCatalog.Claim(...)` to refuse with a domain error when `EventStatus` is Cancelled or Archived.
- [x] 5.4 Domain-event handler within Registrations: when `TicketedEventStatusChanged` is raised, project to `TicketCatalog.EventStatus` in the *same unit of work*.
- [x] 5.5 EF migration for Registrations: drop guard/policy tables, add `TicketedEvent` table with unique index, add `EventStatus` column to `TicketCatalog`.
- [x] 5.6 Unit tests for `EventStatus` transitions, claim refusal when not Active, in-UoW projection from lifecycle change.

## 6. Registrations module — event creation/lifecycle handlers

- [x] 6.1 Integration-event handler for `TicketedEventCreationRequested`: attempt to insert `TicketedEvent`; on success create `TicketCatalog` (Active) in the same UoW and outbox `TicketedEventCreated`; on `(TeamId, Slug)` unique-violation outbox `TicketedEventCreationRejected` with reason `duplicate_slug`.
- [x] 6.2 `CancelTicketedEventCommand` + handler: load aggregate, call `Cancel()`, persist (which projects to `TicketCatalog`), outbox `TicketedEventCancelled`.
- [x] 6.3 `ArchiveTicketedEventCommand` + handler: load aggregate, call `Archive()`, persist (projects to catalog), outbox `TicketedEventArchived`.
- [x] 6.4 `UpdateTicketedEventDetailsCommand` + handler with optimistic concurrency on `Version`.
- [x] 6.5 Application tests covering happy path creation, duplicate-slug rejection, cancel/archive lifecycle including concurrent claim races.

## 7. Registrations module — policy commands & endpoints

- [x] 7.1 `ConfigureRegistrationPolicyCommand` + handler + endpoint `PUT /admin/teams/{teamSlug}/events/{eventSlug}/registration-policy`.
- [x] 7.2 `ConfigureCancellationPolicyCommand` + handler + endpoint `PUT /admin/teams/{teamSlug}/events/{eventSlug}/cancellation-policy` (supports clearing the policy).
- [x] 7.3 `ConfigureReconfirmPolicyCommand` + handler + endpoint `PUT /admin/teams/{teamSlug}/events/{eventSlug}/reconfirm-policy` (supports clearing the policy).
- [x] 7.4 FluentValidation validators for each command (window ordering, positive cadence, etc.).
- [x] 7.5 Read endpoints to fetch current policies as part of `GET /admin/teams/{teamSlug}/events/{eventSlug}` event detail response.
- [x] 7.6 Application tests for each policy command including reject-when-not-Active.

## 8. Registrations module — registration handler updates

- [x] 8.1 Update self-registration command handler to load `TicketedEvent` (for window + domain + active-status policy checks) and `TicketCatalog` (for atomic claim) in the same UoW.
- [x] 8.2 Update coupon-registration command handler likewise; coupon SHALL bypass capacity/window/domain per existing rules but SHALL NOT bypass the active-status gate.
- [x] 8.3 Translate `TicketCatalog.Claim` Cancelled/Archived domain error to a "event not active" rejection at the application layer.
- [x] 8.4 Remove all references to `TicketedEventLifecycleGuard` from registration code.
- [x] 8.5 Update existing attendee-registration tests to seed `TicketedEvent` + `TicketCatalog` instead of guards/policies. Add new tests for the concurrent-cancel race (status flips between policy check and claim).

## 9. Registrations module — ticket type management updates

- [x] 9.1 Update `AddTicketType`, `UpdateTicketType`, `CancelTicketType` handlers to reject when `TicketCatalog.EventStatus` is not Active (replacing guard checks).
- [x] 9.2 Remove the "create catalog on first ticket type" path; catalog is now created during event materialisation.
- [x] 9.3 Update tests accordingly.

## 10. Registrations module — derived registration-openness reads

- [x] 10.1 Add a derived "is-registration-open" computation (window AND status==Active) and expose via the public/event-detail read APIs as needed.
- [x] 10.2 Tests for the derivation matrix (before/in/after window × Active/Cancelled/Archived).

## 11. Cross-cutting cleanup

- [x] 11.1 Delete the orphaned files: `EventRegistrationPolicy.cs`, `EventCancellationPolicy.cs`, `EventReconfirmPolicy.cs`, `TicketedEventLifecycleGuard.cs`, and the legacy Organization `TicketedEvent` files (already noted under modules — confirm zero references via solution-wide search).
- [x] 11.2 Search the solution for `TicketedEventLifecycleGuard`, `PolicyMutationCount`, and `EventRegistrationPolicy`/`EventCancellationPolicy`/`EventReconfirmPolicy` symbols and remove every reference (DI registration, tests, fixtures).
- [x] 11.3 Update arc42 chapters 5 (building blocks), 6 (runtime view — new create flow + cancel/archive flow), and 8 (cross-cutting concepts — update the consistency model and messaging taxonomy).
- [x] 11.4 Add an ADR documenting "TicketedEvent ownership moved to Registrations + EventStatus projection on TicketCatalog" and link from chapter 9.

## 12. Admin UI — async event creation

- [x] 12.1 Update create-event form submission to handle `202 Accepted` + `Location` header; store the creation-status URL in component state.
- [x] 12.2 Implement polling hook (initial 500ms, exponential backoff to ~2s, stop on terminal status or configurable max-wait).
- [x] 12.3 While polling, disable the form and show a non-blocking spinner/progress bar/something that looks modern and nice.
- [x] 12.4 On `Created`, navigate to `/teams/{teamSlug}/events/{eventSlug}/settings`.
- [x] 12.5 On `Rejected` with `duplicate_slug`, attach the inline error to the slug field; on other rejection reasons, surface as a top-of-form error.
- [x] 12.6 On `Expired`, render a "creation timed out, please try again" error and re-enable the form.

## 13. Admin UI — policy pages & event header

- [x] 13.1 Update Registration Policy page to read/write via the new endpoints; remove any "Open/Close registration" controls or status toggle; submit with `TicketedEvent.Version`.
- [x] 13.2 Update Cancellation Policy page similarly with cleared/set states.
- [x] 13.3 Update Reconfirm Policy page similarly with client-side validation (close-after-open, cadence ≥ 1).
- [x] 13.4 Make all three policy pages read-only with a banner when `TicketedEvent.Status` is Cancelled or Archived.
- [x] 13.5 Event detail header: render status badge from `TicketedEvent.Status`.

## 14. CLI

- [x] 14.1 Regenerate the NSwag `ApiClient` against the new endpoints (use the `cli-api-client-generation` skill / approved workflow).
- [x] 14.2 Update `events create` CLI command to call the new async endpoint and add a `--wait` / `wait-for-creation` helper that polls the creation-status URL with a configurable timeout (default 30s).
- [x] 14.3 Update or add CLI commands for the three policy endpoints (registration / cancellation / reconfirm) so each admin endpoint has CLI parity per `src/Admitto.Cli/AGENTS.md`.

## 15. End-to-end verification

- [x] 15.1 Run all module domain + application test suites and Admitto.Api.Tests; fix any breakage.
- [ ] 15.2 Manually smoke through the create→configure-policies→register→cancel flow via Aspire local dev, confirming the UI's polling UX, idempotent integration-event handling on redelivery, and team-archive being blocked by pending/active counts.
- [x] 15.3 Run `openspec validate redesign-ticketed-event-ownership --strict` and resolve any drift between code and specs before archiving.
