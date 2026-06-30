## Context

The Email module sends application-owned transactional and bulk email through deployment-provided system SMTP settings. Current architecture keeps the actual sender address Admitto-controlled, while team-specific context is projected into `email.team_email_context_view` and read through `IEmailReadStore`.

Today, the MIME `From` display name is derived from the team's reply-to email address when one exists. The team name is already part of the Email-owned team context projection, so the behavior can change without synchronously querying Organization or adding a cross-module dependency.

## Goals / Non-Goals

**Goals:**

- Use the projected team name as the visible MIME `From` display name for transactional and bulk application email.
- Keep the configured Admitto-controlled sender address as the actual MIME/SMTP `From` address.
- Keep the optional team reply-to email address only for the `Reply-To` header.
- Preserve Email module ownership of rendering and sender context via projections.

**Non-Goals:**

- Do not make organizer-owned SMTP settings configurable again.
- Do not change reply routing or the team reply-to management API.
- Do not introduce synchronous Organization reads into the Email sending path.
- Do not change Keycloak account-action email behavior.

## Decisions

1. Resolve sender display name from Email's team context projection.

   The effective settings resolver should read both `TeamName` and `ReplyToEmailAddress` from `IEmailReadStore.TeamEmailContexts`. This keeps Email's existing projection ownership model intact. Alternative considered: query Organization when sending email. Rejected because the architecture requires Email to use its own projections for reusable sender and rendering context.

2. Add an explicit sender display-name value to effective email settings.

   Message construction should not infer the display name from `ReplyToAddress`. The resolved settings should carry a separate display-name value so `From` labeling and `Reply-To` routing remain independent. Alternative considered: pass `TeamName` separately to every send call. Rejected because both transactional and bulk send paths already depend on `EffectiveEmailSettings`, and centralizing the value avoids divergent behavior.

3. Fall back to the configured sender address when team name is unavailable.

   Partial or out-of-order projection rows can exist. If a team context row or team name is missing, the message should still use the configured Admitto sender address as the display name rather than failing a send solely because labeling context has not arrived. Alternative considered: treat missing team name as deterministic render-context failure. Rejected because this change is sender labeling, not registration correctness, and existing behavior already tolerates partial projection context for sender metadata.

## Risks / Trade-offs

- [Risk] Recently changed team names may lag in Email's projection and an email can use the previous name. → Mitigation: document this as the existing eventual-consistency behavior for Email-owned context.
- [Risk] Partial projection rows may not have a team name at send time. → Mitigation: fall back to the configured Admitto sender address display name.
- [Risk] Tests may only cover the single-send path. → Mitigation: update both direct MIME message construction tests and bulk path tests so transactional and bulk sends remain consistent.

## Migration Plan

No data migration is required because `TeamEmailContextView` already stores `TeamName` and Organization integration events already project it. Deploying the code changes is sufficient.

Rollback is code-only: restoring the previous MIME builder/settings behavior returns display names to the reply-to address while leaving persisted projections unchanged.

## Open Questions

None.
