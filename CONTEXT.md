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
