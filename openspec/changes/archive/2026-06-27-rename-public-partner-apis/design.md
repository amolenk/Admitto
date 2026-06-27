## Context

The API host currently exposes two public-facing route groups from `PublicEndpoints`: an API-key-protected group under `/api/...` and an anonymous group under `/e/...`. The `/api/...` group is documented and tagged as the Public API, but it is actually intended for partner event websites and requires `X-Api-Key`. The anonymous `/e/...` route currently contains only `ResolvePublicEventLink` and redirects `/e/{publicSlug}` to the event website URL.

Email templates need links that work directly for attendees and mail clients without API keys. This makes `/e/...` the correct Public API surface. The API-key-protected `/api/...` surface should be renamed to Partner API while preserving route compatibility for existing partner websites.

Azure Container Apps can serve `tickets.admitto.org` as a second custom domain for the API container app. This is a host alias, not an HTTP redirect. For managed certificate issuance and renewal, the subdomain CNAME should point directly to the generated Container Apps hostname and the custom domain must be bound to the app with its own certificate.

## Goals / Non-Goals

**Goals:**

- Establish clear naming for the three trust boundaries: Admin API, Partner API, and Public API.
- Keep existing Partner API routes under `/api/...` intact except for QR-code retrieval.
- Move QR-code retrieval to anonymous `/e/{eventSlug}/qr-code/{registrationId}` routing so it can be embedded in attendee emails.
- Rename the direct-link slice from `ResolvePublicEventLink` to `DirectPublicEventLinks`.
- Keep endpoint logic thin by moving QR-code lookup/generation into query and handler classes.
- Update specs, architecture docs, route tags, tests, and generated API naming where affected.

**Non-Goals:**

- No change to Admin API authentication or authorization.
- No change to Partner API event-scoped route shape beyond removing the QR-code endpoint.
- No new public web frontend; the Public API redirects to configured partner website URLs or returns QR-code PNGs.
- No QR-code revocation or signature scheme.
- No DNS or Azure infrastructure automation in this change.

## Decisions

### Decision: Preserve `/api/...` routes as Partner API routes

The API-key-protected surface will be renamed in code/docs/tags from Public API to Partner API, but existing `/api/events/{eventId}/...` routes remain unchanged.

Alternatives considered:

- Rename routes to `/partner/...`: clearer but breaks existing partner websites and requires a migration/versioning plan.
- Keep the Public API name: avoids churn but continues conflating API-key-protected partner calls with anonymous attendee links.

### Decision: Use `/e/{eventSlug}` as the anonymous Public API root

The Public API is the attendee-facing anonymous route family. It resolves an Admitto-owned public event slug and either redirects to the configured partner website path or returns QR-code image content.

Alternatives considered:

- Put anonymous routes under `/public/...`: semantically clear but less suitable for concise email links and already conflicts with the existing `/e/...` public event-link convention.
- Keep only the canonical event redirect: insufficient for direct email links to registration, cancellation, edit, and QR-code retrieval.

### Decision: Name the slice `DirectPublicEventLinks`

The slice name should describe the user-facing capability, not the internal operation of resolving a slug. The slice will own redirect route mapping and link-target construction for event, register, cancel, and edit links.

Alternatives considered:

- Keep `ResolvePublicEventLink`: accurate for implementation but too narrow now that the capability includes multiple direct links.
- Create one slice per route: more granular but adds indirection for closely related routing behavior that shares the same event slug lookup and URL-joining rules.

### Decision: QR-code retrieval gets its own query/handler but is mounted on Public API routing

The QR-code endpoint will use a `GetQRCodeQuery` and `GetQRCodeHandler` so lookup and generation are testable outside the HTTP endpoint. The endpoint will pass `eventSlug` and `registrationId`; the handler resolves the ticketed event by public slug and checks that the registration belongs to that event.

Alternatives considered:

- Keep the current endpoint-local logic: preserves minimal files but conflicts with established use-case slice conventions.
- Fold QR-code generation into `DirectPublicEventLinks`: mixes binary content generation with redirect-link behavior.

### Decision: Build redirect targets from the stored event website URL as a base path

The existing `TicketedEvent.WebsiteUrl` will be treated as the event website base URL. Redirects append relative path segments to that URL while preserving any existing path prefix.

Examples:

- `https://partner.example/events/azure-fest` + `register` becomes `https://partner.example/events/azure-fest/register`.
- `https://partner.example/events/azure-fest/` + `cancel/{registrationId}` becomes `https://partner.example/events/azure-fest/cancel/{registrationId}`.

Alternatives considered:

- Use only the origin as the base URL: breaks partners whose event pages live under a path.
- Store separate URLs per action: more flexible but requires new event configuration and data-model changes that are not needed yet.

## Risks / Trade-offs

- Anonymous QR-code route makes registration existence observable for guessed GUIDs → Treat registration IDs as attendee-held bearer identifiers, return only normal 404s, and avoid exposing additional details.
- Removing the API-key-protected QR-code route is breaking → Document it explicitly and cover old route behavior in API tests.
- URL path joining can accidentally double-encode or drop existing paths → Use `UriBuilder` or equivalent controlled path construction and test base URLs with and without trailing slashes.
- Open redirect risk if request input controls redirect targets → Only use stored `TicketedEvent.WebsiteUrl`; ignore query-string redirect targets.
- Azure custom domain setup can be misunderstood as a redirect → Document `tickets.admitto.org` as a bound Container Apps custom domain/host alias with direct CNAME and certificate binding.

## Migration Plan

1. Rename code namespaces/classes/tags/tests from Public API to Partner API for API-key-protected `/api/...` endpoints.
2. Rename `ResolvePublicEventLink` to `DirectPublicEventLinks` and add the new redirect routes.
3. Move QR-code routing from Partner API to Public API and introduce query/handler classes.
4. Update tests for new anonymous routes, removed old QR-code route, and preserved Partner API route behavior.
5. Regenerate affected OpenAPI/Admin UI SDK artifacts if endpoint metadata changes require it.
6. Update architecture docs for API trust boundaries and Azure Container Apps custom-domain guidance.

Rollback strategy: keep code changes isolated to endpoint mapping and use-case slices so the old API-key-protected QR-code route could be reintroduced temporarily if an external consumer is discovered.

## Open Questions

- Should the old API-key-protected QR-code route return `404` after removal or remain as a temporary redirect/compatibility endpoint for one release?
- Should QR-code image responses include `Cache-Control: no-store`, or may they be cached by email clients/CDNs?
