## Context

Partner attendee endpoints are mounted under `/api/events/{eventSlug}/...`, require `X-Api-Key`, derive `TeamId` from the API-key principal, and resolve the public event slug within that team scope before invoking Registrations handlers. The current self-service mutation endpoint only replaces an existing registration's ticket selection at `PUT /api/events/{eventSlug}/registrations/{registrationId}/tickets`.

Registration creation already accepts first name, last name, ticket selections, and optional additional details in one request. The `Registration` aggregate already persists `FirstName`, `LastName`, `Tickets`, and `AdditionalDetails`, and additional details are validated against the event-owned `AdditionalDetailSchema`.

## Goals / Non-Goals

**Goals:**

- Replace the ticket-only Partner API mutation with a single self-service registration update endpoint.
- Persist first name, last name, additional details, and ticket selection atomically.
- Reuse existing Partner API scoping and registration-id bearer-link semantics.
- Reuse existing additional-detail validation rules from registration creation.
- Keep waitlist coupon support for changing an existing registration into an offered ticket.

**Non-Goals:**

- No backward-compatible `/tickets` endpoint or adapter.
- No new persisted registration fields.
- No new anonymous public endpoint under `/e/...`.
- No email-address update; email remains the registration identity and coupon/waitlist target address.
- No separate attendee edit window distinct from the existing registration window.

## Decisions

1. Replace the route with `PUT /api/events/{eventSlug}/registrations/{registrationId}`.

   This models the operation as replacing the attendee-editable registration state rather than editing only one child collection. Alternative considered: keep `/tickets` and add fields to that request. That was rejected because the route name would no longer describe the operation and the user explicitly does not require backward compatibility.

2. Implement the endpoint as a new/different use-case slice.

   The broader mutation should live in its own Registrations slice, for example `Application/UseCases/Registrations/UpdatePartnerRegistration/`, with its own command, handler, Partner API request, validator, and endpoint. The existing `ChangeAttendeeTickets` slice should not be stretched to own attendee identity and additional-detail updates. Alternative considered: rename or widen `ChangeAttendeeTickets`. That was rejected because the old slice's concept and side effects are ticket-specific, while this operation replaces the attendee-editable registration state.

3. Treat the request as a full self-service update payload.

   The request should require `firstName`, `lastName`, and `ticketTypeIds`. `additionalDetails` remains optional, matching registration creation; omitted or `null` means no additional detail values are submitted. Alternative considered: patch-style partial updates. That was rejected because the existing registration create flow submits one complete form and because partial semantics create ambiguity around deleting additional-detail values.

4. Continue using `registrationId` plus Partner API key as the mutation credential.

   Registration-bound public links are already documented as high-entropy bearer secrets, and the Partner API key scopes the event website integration to a team. The email-verification bearer token remains necessary for lookup-by-email detail reads because email is enumerable; it is not added to this registration-id mutation path. Alternative considered: require email-verification tokens for name/detail updates only. That was rejected because a single call should have one authorization model and because the registration-id link is the existing attendee credential for self-service mutation.

5. Validate additional details against the current event schema at update time.

   This matches registration creation: unknown keys and over-length values are rejected, missing keys are accepted, and empty strings are preserved. If a previously stored key was removed from the schema, the attendee can no longer submit it; because this is a full replacement, omitting it removes it from the registration's current additional details.

6. Emit ticket-change side effects only for actual ticket-set changes.

   The broader endpoint can change only name/details while leaving tickets unchanged. Sending a ticket confirmation email for a details-only edit would be misleading, so ticket-change domain/integration events should be raised only when the ticket selection differs. Detail-only persistence does not introduce a new cross-module integration event in this change.

## Risks / Trade-offs

- Existing partner websites using `/registrations/{registrationId}/tickets` will break -> This is accepted by the change scope; callers must migrate to the new route and full request body.
- Details-only edits require a valid registration window -> This preserves existing self-service ticket-change constraints but may be stricter than some organizer expectations; a separate edit-window policy can be proposed later if needed.
- Full replacement can remove historical additional-detail keys -> This is intentional for attendee edits, while the schema-removal rule still preserves untouched historical registrations until an attendee resubmits the full detail map.
- No new integration event for detail changes -> Downstream modules will not react to first-name/last-name/additional-detail edits unless they query registration data live or a future event is introduced.
