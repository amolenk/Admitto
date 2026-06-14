## Why

External registration websites currently receive self-registration failures as generic problem responses. When a requested ticket sells out or leaves waitlist mode between page load and submit, the website cannot reliably tell the attendee which parts of the selection can still be registered, which can be waitlisted, and which are unavailable.

Providing structured ticket-state details for recoverable self-registration conflicts lets external websites turn a failed submit into an explicit user choice instead of a dead-end error.

## What Changes

- Add a structured self-registration ticket-state conflict response for public self-service registration failures caused by changed ticket availability.
- Include grouped ticket-state arrays that identify which submitted ticket type ids are registerable, waitlistable, unavailable, unknown, or invalid for the submitted action.
- Keep the existing all-or-nothing registration semantics: the failed request still persists no registration, waitlist entry, or capacity change.
- Preserve token validation behavior: missing, invalid, expired, or mismatched verification tokens are still rejected before event, catalog, waitlist, or ticket-type lookups and do not expose ticket-state details.
- Preserve terminal failure behavior for non-ticket-state errors such as event not active, registration window closed, email-domain rejection, duplicate active registration, and additional-detail validation.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `attendee-registration`: Public self-registration SHALL return structured ticket-state details for recoverable ticket-selection conflicts.

## Impact

- Public self-service registration API response shape for selected conflict cases.
- Registrations module self-service registration use case and endpoint error mapping.
- Shared HTTP problem-details handling may need to expose selected error details safely.
- API and integration tests for recoverable ticket-state conflicts.
- Generated API clients or external documentation may need regeneration after the OpenAPI contract changes.
