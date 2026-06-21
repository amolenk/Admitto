## Context

Public self-service registration already accepts two explicit attendee intents: `registerTicketTypeIds` for tickets the attendee wants now and `waitlistTicketTypeIds` for ticket types whose waitlists the attendee wants to join. The handler validates the request atomically and rejects the whole request when any requested action cannot be applied exactly as submitted.

That all-or-nothing behavior is correct, but the public error contract is too coarse for external websites. A ticket can move from registerable to sold out or waitlist-only between page load and submit. The external site needs enough structured detail to ask the attendee whether to continue with available tickets and explicitly join waitlists for sold-out tickets.

Relevant constraints from the architecture:

- The Registrations module owns ticket availability, `TicketCatalog`, waitlist mode, and the public self-service registration use case.
- Endpoint handlers own the transaction boundary; command handlers must not commit.
- Verification-token failures must remain non-enumerating and run before event/catalog/ticket lookups.
- `TicketCatalog` remains the atomic active-status and capacity gate.

## Goals / Non-Goals

**Goals:**

- Return grouped ticket-state arrays for recoverable self-registration ticket-selection conflicts.
- Let external websites classify originally requested tickets into simple buckets: registerable, waitlistable, unavailable, unknown, or invalid for the submitted action.
- Preserve explicit consent: the API must not silently convert a registration request into a waitlist join.
- Preserve atomicity: failed submissions still persist no partial registration, waitlist entry, or capacity change.

**Non-Goals:**

- Automatically resubmitting a revised registration or joining waitlists on behalf of the attendee.
- Changing coupon registration behavior.
- Changing the public ticket-list query except where the implementation needs shared DTOs or helpers.
- Exposing ticket-state details for authentication, authorization, event lifecycle, domain, duplicate-registration, or additional-detail failures.

## Decisions

### Use a dedicated 409 problem-details contract for ticket-state conflicts

Recoverable ticket-selection failures should be returned as HTTP 409 with a stable code such as `registration.ticket_state_conflict`. The problem response should include a typed extension with grouped arrays of submitted ticket type ids.

Alternative considered: continue returning existing domain errors and require clients to refetch public ticket types. This is simpler server-side, but it forces every external website to reverse-engineer the changed selection and loses information such as unknown IDs or action-specific invalidity.

### Report current state as grouped arrays

The conflict extension should include arrays such as:

- `registerableTicketTypeIds`
- `waitlistableTicketTypeIds`
- `unavailableTicketTypeIds`
- `unknownTicketTypeIds`
- `invalidForRequestedActionTicketTypeIds`

Grouped arrays are intentionally coarse. External websites need to decide which tickets can remain in the registration selection, which tickets can be offered as waitlist joins, and which tickets must be removed. They do not need a per-ticket object with action/reason metadata.

Alternative considered: return one object per submitted ticket with `requestedAction`, `state`, and `reasonCode`. That is more extensible, but it adds parsing overhead and exposes more nuance than external websites currently need.

### Keep terminal validation failures on their current error path

The structured ticket-state response should only cover conflicts where revising the ticket action could allow the same attendee to continue. Token failures, event inactive, registration window closed, email-domain rejection, duplicate active registration, and additional-detail validation should keep their existing errors.

Alternative considered: return a full form-state diagnostic for every self-registration error. That would be convenient for some clients, but it risks unnecessary information disclosure and expands this change beyond ticket availability.

### Compute states from the authoritative catalog at failure time

The response should be derived from the same `TicketCatalog` state used by self-registration validation. For requested registration tickets, a sold-out waitlist-enabled type in waitlist mode should be reported as waitlistable rather than merely failed.

This does not change the capacity claim rule: a ticket is only actually reserved after a successful claim and commit. The conflict response is advisory UI state, not a reservation.

## Risks / Trade-offs

- [Race after conflict response] Ticket state can change again before the attendee resubmits → Clients must treat the response as advisory and handle another 409.
- [Information disclosure] Unknown or hidden ticket IDs could leak catalog details → Only return submitted IDs grouped by state, and keep token validation before catalog lookup.
- [Client compatibility] Existing clients may assume minimal problem details → Use additive problem-details extensions and keep status/code semantics stable for conflicts.
- [OpenAPI churn] Generated SDKs need refreshing after the response schema changes → Include SDK regeneration in implementation tasks where applicable.

## Migration Plan

This is an additive API-contract change for recoverable self-registration conflicts. Existing clients that only inspect HTTP status and problem `code` can continue to show a generic error. New or updated clients can inspect the ticket-state extension and offer a revised selection flow.

Rollback is straightforward: clients should tolerate the extension being absent and fall back to refetching public ticket types or showing the generic conflict.

## Open Questions

- Should the response include ticket display names, or should clients use their existing/refetched public ticket list for display copy?
