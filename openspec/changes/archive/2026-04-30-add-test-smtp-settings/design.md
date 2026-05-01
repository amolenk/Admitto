## Context

The Admin UI has team-scoped and event-scoped email-settings pages that already use the slice family in `src/Admitto.Module.Email/Application/UseCases/EmailSettings/` (Get, UpsertCreate, UpsertUpdate, Delete). Saved settings drive the existing send pipeline (`SendEmailCommandHandler` + `EffectiveEmailSettingsResolver`), which falls back from event to team scope and writes every send to `email_log`. Today, an organizer who saves wrong SMTP credentials only finds out when a real registration email fails — long after the misconfiguration was made.

This change introduces a synchronous "send a diagnostic email" admin endpoint, an Admin-UI action that drives it, and a CLI command for parity. It deliberately reuses everything in the send pipeline that is reusable (`IEmailSender`, `IProtectedSecret`, `EffectiveEmailSettings`) but does **not** reuse `EffectiveEmailSettingsResolver` (which falls back) or `SendEmailCommandHandler` (which logs and is asynchronous).

## Goals / Non-Goals

**Goals:**
- Give organizers immediate, in-page success/failure feedback when verifying SMTP credentials, at both team and event scope.
- Surface real SMTP transport errors (auth failure, connection refused, certificate errors) as the message body of the response so the user can fix the configuration.
- Test exactly the settings the chosen scope owns — never silently fall back to the team scope when the event scope is being verified.
- Add a CLI command with the same shape as the existing `email`-family commands and use the regenerated `ApiClient` as the only HTTP boundary.
- Avoid any new cross-module contract or any new persistence.

**Non-Goals:**
- Testing unsaved form values. Users save first, then test. (Confirmed UX trade-off — keeps the endpoint trivial, avoids password-merging logic, and matches the existing Delete UX.)
- Recording the diagnostic in `email_log`. The log is for real correspondence; cluttering it with diagnostics would distort reporting.
- Server-side validation that the chosen recipient is a member of the team or matches the team's contact email. The recipient is provided by the UI, which already pulls those addresses from the existing admin endpoints; the API trusts the authenticated organizer. The send is an authenticated, fixed-content message bounded by user clicks and is not a useful spam vector.
- Inheritance display on the event page (already covered by the existing `Inherited from team settings` callout). The test action is hidden in the inherited case so the user is not misled about which row is being verified.

## Decisions

### Decision: Synchronous send via `IEmailSender`, no outbox, no `email_log`

The endpoint awaits `IEmailSender.SendAsync(...)` directly and returns the result on the same HTTP response. The user has clicked a button and is waiting; the value of immediate feedback dominates the cost of holding a request open for the duration of an SMTP handshake.

We considered enqueuing through the existing `IMessageOutbox` (the path used by real sends and by the legacy `TestEmailEndpoint`), but the outbox returns `Accepted` and the user would then need a separate way to find the result — which neither the outbox nor `email_log` exposes today. Building a per-test result projection is far more complex than the synchronous call.

We also considered writing to `email_log` for completeness. The log models attendee correspondence (recipient, subject, status). A diagnostic does not have any of those semantics and would skew per-event delivery metrics. The handler therefore writes nothing to the database.

### Decision: Read settings directly, no `EffectiveEmailSettingsResolver`

The handler reads exactly one row — `EmailSettings` for the requested `(Scope, ScopeId)` — and rejects the request if the row is missing. It then mirrors `EffectiveEmailSettingsResolver.ToEffective(...)` inline (decrypt password via `IProtectedSecret.Unprotect`, build an `EffectiveEmailSettings`).

Reusing `EffectiveEmailSettingsResolver` would silently fall back to the team scope when the event-scoped row is absent. That breaks the contract of the test ("verify these settings"): the user might add wrong settings at the event scope, see a successful test (because the team scope works), and ship the broken configuration. Direct read avoids this entirely.

### Decision: Recipient passed in the request body; UI resolves the choice

The endpoint accepts a `recipient` field in the body and the UI populates a dropdown from the team's contact email + team-member emails — both already fetched via existing admin endpoints (`/admin/teams/{teamSlug}` and `/admin/teams/{teamSlug}/members`). This avoids extending `IOrganizationFacade` with a new method (`GetTeamEmailAddressAsync`) and keeps the Email module ignorant of team membership.

We considered hard-coding the recipient to "the team's contact email" inside the API. That option requires either a new facade method (an Email→Organization dependency where there is none today for membership/email lookup) or a duplicate read against the org schema. It also denies the user a useful affordance: today, when an organizer is debugging Gmail SMTP, "send to my work address" is a more meaningful test than "send to the generic events inbox."

We considered server-side validation that the chosen recipient belongs to the same team. That re-introduces the cross-module call we are avoiding. The trade-off is documented in Goals/Non-Goals: the diagnostic content is fixed, the endpoint is authenticated and rate-limited by user clicks, and the recipient is chosen from a UI dropdown sourced from existing endpoints. There is no spam vector worth the additional coupling.

### Decision: Endpoint mapped through the same scope-parameterised helper as the existing settings endpoints

`MapSendTestEmail(this RouteGroupBuilder group, EmailSettingsScope scope, Func<OrganizationScope, Guid> scopeIdSelector)` is registered next to `MapDeleteEmailSettings` in `EmailApiEndpoints.cs`, on both the team and event groups. This keeps the four-endpoint slice family (Get/Upsert/Delete/Test) symmetric and means the routing, scope resolution, and authorization wiring are identical at the two scopes — the same pattern that has already paid off for `MapUpsertEmailSettings`.

### Decision: One reusable UI button component, used on both pages

Both pages already share `EmailSettingsForm`. We add a sibling component, `TestEmailSettingsButton`, that takes (a) the API URL for the current scope and (b) the list of recipient options. Each page resolves the options from its own queries against `/api/teams/{teamSlug}` and `/api/teams/{teamSlug}/members` and passes them in. The button owns the recipient `Select`, the loading state, and the inline result `Alert` (non-destructive on success, destructive on failure).

We considered adding the action to `EmailSettingsForm` itself. That would couple the form to the test API, the recipient queries, and the inheritance state of the page (the event page must not show the test action when inheriting from the team scope). Keeping the button outside the form preserves the form's role as a single-purpose CRUD widget.

### Decision: Validate the recipient in a FluentValidation validator at the endpoint filter

The single body field is validated by a `SendTestEmailValidator` registered alongside the existing settings validators, matching the project convention that "Admin routes run FluentValidation in the endpoint filter before handler execution" (see `AGENTS.md`). The handler receives an already-parsed `EmailAddress` and never sees malformed input.

## Risks / Trade-offs

- **[Risk] Long-blocking SMTP handshake holds an HTTP request open.** A misconfigured host can take many seconds to fail (DNS timeout, TCP timeout, TLS negotiation). → **Mitigation**: `MailKit`'s `SmtpClient.ConnectAsync`/`AuthenticateAsync` accept the request `CancellationToken`; the handler passes it through, so a client cancel disconnects cleanly. We will not introduce a custom shorter timeout in this change — the underlying defaults are acceptable for an interactive diagnostic and matching them to the real send path keeps behaviour predictable.
- **[Risk] Authenticated organizer can send a diagnostic to any address.** → **Mitigation**: content is fixed, volume is per-click, and the recipient is chosen from a UI dropdown sourced from existing org admin endpoints. No realistic spam vector. If abuse becomes a concern later, recipient can be locked down to the team's address set with a single small endpoint change without breaking the UI contract.
- **[Trade-off] Diagnostic does not appear in `email_log`.** Operators looking at the log will not see test sends. We accept this because it keeps the log dedicated to real correspondence (delivery analytics, support tickets), and the success/failure result is shown in the UI at the moment of the test.
- **[Trade-off] Test verifies only saved settings, not the in-form values.** Users with unsaved edits must save first. This matches the existing `Delete` UX and avoids both password-merging logic and a richer request DTO. If a strong need emerges to test unsaved values, the endpoint can be extended later by accepting an optional full `SmtpSettingsDto` that takes precedence over the stored row.

## Migration Plan

No data migration. No feature flag. Endpoint is additive; UI changes are additive (button only renders when settings exist). On rollback, the new endpoint returns 404 and the UI button still renders but its calls fail with a server error — the rest of the email-settings UX is unaffected.

## Open Questions

None.
