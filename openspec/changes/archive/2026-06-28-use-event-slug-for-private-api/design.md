## Context

The Partner API is mounted under `/api/...`, requires `X-Api-Key`, and currently identifies event-scoped resources with `/api/events/{eventId}`. API-key authentication already resolves the owning team into a claim, so event lookup is always team-scoped even when the route uses a globally unique event identifier.

`TicketedEvent.PublicSlug` already exists and is used by anonymous public links under `/e/{publicSlug}`. Moving the Partner API to `/api/events/{eventSlug}` aligns external website integration with the public identifier while keeping `TicketedEventId` as the internal aggregate key used by handlers, persistence, tokens, outbox messages, and cross-module contracts.

## Goals / Non-Goals

**Goals:**

- Make every event-scoped Partner API route accept the public event slug instead of the event ID.
- Resolve the slug inside the API-key owner's team scope and pass the resolved `TicketedEventId` to existing application handlers.
- Preserve the current authorization and not-found behavior for missing API keys, invalid API keys, unknown events, and cross-team mismatches.
- Update OpenAPI, generated clients, tests, and architecture docs to reflect the route contract.

**Non-Goals:**

- Changing aggregate identities, database foreign keys, integration events, or verification-token claims away from `TicketedEventId`.
- Adding compatibility routes for `/api/events/{eventId}`.
- Changing anonymous `/e/{publicSlug}` routes or admin `/admin/...` routes.
- Changing API-key ownership or team-scoping semantics.

## Decisions

### Resolve Slug At The HTTP Boundary

Partner endpoints SHALL accept `string eventSlug`, resolve it to `(TeamId, TicketedEventId)` before creating commands/queries, and continue passing `TicketedEventId` into handlers.

Rationale: this keeps existing domain/application code and persisted references stable while limiting URL-contract changes to the HTTP boundary.

Alternative considered: change commands and handlers to accept slugs. This would spread routing concerns through application logic and require repeated slug resolution in handlers that already operate on aggregate IDs.

### Scope Slug Lookup By API-Key Team

Slug resolution SHALL use the `TeamId` from the authenticated API-key principal and `TicketedEvent.PublicSlug`. Unknown slugs and slugs belonging to another team SHALL return the same not-found behavior as today's unknown or wrong-team event IDs.

Rationale: this preserves the existing fail-closed team scope and avoids leaking whether a slug exists for another team.

Alternative considered: resolve globally by slug first, then compare team ownership. This is unnecessary and makes it easier to introduce different error paths for cross-team slugs.

### Do Not Support Both Route Shapes

The old `/api/events/{eventId}` routes SHALL be removed rather than retained as aliases.

Rationale: the requested change is a breaking external API cleanup. Supporting both shapes adds route ambiguity, OpenAPI duplication, and test burden without a documented compatibility need.

Alternative considered: temporarily support both event ID and slug. This would require ambiguity handling for slug strings that parse as GUIDs and would preserve the internal ID exposure the change is intended to remove.

### Keep Verification Tokens Bound To Event ID

OTP verification SHALL resolve `{eventSlug}` to `TicketedEventId` before issuing or validating verification tokens. Token claims continue to contain `eventId` and `teamId`.

Rationale: tokens are internal signed credentials. Keeping immutable IDs in token claims avoids coupling token validation to mutable route text and preserves existing handler semantics.

Alternative considered: store the slug in verification tokens. This would make token validation sensitive to slug changes and require broader token model changes.

## Risks / Trade-offs

- Existing partner websites using `/api/events/{eventId}` will break until updated -> communicate the breaking route change and regenerate client SDKs from the updated OpenAPI spec.
- Route parameter names may change generated SDK function parameter shapes -> regenerate affected SDKs and update proxy/client call sites rather than hand-coding replacements.
- Slug resolution adds one lookup to each Partner API request -> keep the lookup indexed by `(TeamId, PublicSlug)` or equivalent existing uniqueness/indexing.
- Inconsistent documentation could leave stale `{eventId}` examples -> update OpenSpec, arc42 runtime/cross-cutting docs, and API tests together.

## Migration Plan

1. Update Partner API route groups from `/{eventId:guid}` to `/{eventSlug}`.
2. Add or reuse a Registrations read operation that resolves public slug within API-key team scope to `TicketedEventId`.
3. Update affected endpoint slices to resolve the ID before constructing commands/queries.
4. Update API tests for success, unknown slug, cross-team slug, and old GUID route removal.
5. Regenerate any affected OpenAPI clients and update downstream call sites.
6. Update arc42 sections that describe `/api/events/{eventId}`.

Rollback is a normal code rollback to the previous route templates and generated clients. No data migration is required because aggregate IDs remain unchanged.

## Open Questions

- None.
