# Admitto context

These are the canonical domain terms for reconfirmation and email:

## Requested reconfirmation deadline

An event-level request for when an attendee's reconfirmation opportunity should reach terminal evaluation. It is a requested deadline, not a promise that evaluation occurs at that exact instant.
_Avoid_: Exact close time, guaranteed close instant

## Reconfirmation batch

A bounded set of reconfirmation work evaluated and sent together for one ticketed event. It represents one attempt to contact eligible attendees during their reconfirmation cycle.
_Avoid_: Bulk email job, recipient snapshot

## Transactional email

An email caused by a business action or state change, such as registration confirmation, cancellation, or a reconfirmation request. Reconfirmation is transactional communication even when sent as a batch.
_Avoid_: Marketing email, campaign

## Transactional email composition

The derivation of a transactional email's rendered subject and body from cause-specific facts, event and team context, public links, a built-in template, and team branding. It excludes recipient selection, idempotency, delivery claims, SMTP configuration, and transport.
_Avoid_: Email delivery, email sending, template rendering

## Transactional email intent

A semantic request to compose one kind of transactional email, carrying only the cause-specific facts needed for that message. It is independent of template names, template-variable spelling, delivery claims, and transport.
_Avoid_: Template parameters, email type string, send command

## Coupon invitation

A transactional email inviting a recipient to register for an event with an organizer-issued coupon code. It is distinct from a waitlist offer and therefore carries no waitlist position, ticket type, or expiry language.
_Avoid_: Waitlist notification, waitlist offer
