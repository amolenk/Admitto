# Admitto — Waitlist as a First-Class Registration State

> Companion spec: `BITBASH_SPEC.md` (in the Bitbash repo). That spec depends on the update-registration split request shape and the registration read-model extension described here being implemented first.

## Problem Statement

The self-service registration API already lets an attendee submit a mix of confirmed and waitlisted ticket type selections when they first register, but nothing else in the system treats "on a waitlist" as a first-class, ongoing state of an attendee's registration:

- A waitlist-only signup produces no registration record at all, so that attendee has no way to look up, edit, or cancel their own waitlist entry later, and receives no email confirming anything happened.
- The self-service update-registration endpoint has no concept of waitlisting at all — an attendee editing their registration cannot move between confirmed and waitlisted ticket types.
- The automatic waitlist-promotion path issues a coupon but never actually emails the promoted attendee (a pre-existing defect).
- There is no way for an organizer to manually give a specific, already-waitlisted person priority access (a "VIP" override) without going through the normal first-in-line queue order.
- The system has two different kinds of coupons (organizer-issued and waitlist-issued) with duplicated, slightly inconsistent redemption rules, when their actual behavior should be identical.

## Solution

Waitlisting becomes a first-class, ongoing part of a Registration's lifecycle rather than a one-time, registration-less side effect of signing up: every waitlist join produces a Registration (in a new `Waitlisted` status when it holds no confirmed tickets), self-service update supports moving ticket types between confirmed and waitlisted, all lifecycle-relevant transactional emails are updated to describe this correctly, the broken promotion-notification email is fixed, and a unified coupon model — organizer- and waitlist-issued coupons behave identically at redemption, differing only in which email they trigger at creation — underpins a new admin action letting a team member manually promote a specific waitlisted attendee.

## User Stories

### Organizer / admin-facing

1. As an event organizer, I want a waitlist-only signup to produce a real registration record, so that the attendee has a durable way to manage their own status later.
2. As an event organizer, I want an attendee who is only waitlisted to show up with a distinct `Waitlisted` status in my registration lists and exports, so that I can tell them apart from confirmed attendees at a glance.
3. As an event organizer, I want check-in to refuse a waitlisted (zero-confirmed-ticket) registration, so that nobody without an actual ticket can be scanned in.
4. As an event organizer, I want reconfirmation requests to never be sent to waitlisted registrations, so that I don't ask someone to reconfirm attendance for a ticket they don't have.
5. As an event organizer, I want cancelling a registration to also withdraw any of that attendee's still-active waitlist entries, so that a cancelled attendee can't still receive a promotion offer afterward.
6. As an event organizer, I want to view a specific attendee's identity by drilling into their registration detail from the waitlist list (which only shows masked emails), so that I can act on a specific person without exposing everyone's email addresses in a list view.
7. As an event organizer, I want to manually issue a coupon to one specific person currently on a ticket type's waitlist, out of the normal first-in-line order, so that I can give VIP treatment to someone without having to look up and paste in their email address separately.
8. As an event organizer, I want that manual VIP coupon to immediately remove the person from the waitlist queue, so that they aren't double-counted or accidentally promoted again later.
9. As an event organizer, I want the manual VIP coupon and the automatic queue-promotion coupon to send the same "your spot is ready" email, so that the attendee's experience doesn't depend on how they got promoted.
10. As an event organizer, I want redeeming any coupon — whether I issued it manually for general purposes or it came from the waitlist — to clear any matching waitlist entry the redeemer happens to have for that ticket type, so that nobody stays queued for a ticket type they already got another way.
11. As an event organizer, I want to issue a coupon covering more than one ticket type at once, so that I can invite someone to multiple sessions with a single coupon.
12. As an event organizer, I want an attendee to be able to redeem only some of a multi-ticket-type coupon's ticket types (forfeiting the rest), so that they aren't forced to take every ticket type the coupon happened to list.

### Attendee-facing (backend-observable outcomes, complementing `BITBASH_SPEC.md`'s frontend stories)

13. As an attendee who signs up choosing only ticket types that are full, I want a registration record created for me (even though I hold no confirmed ticket yet), so that I have a durable identity in the system I can later look up.
14. As an attendee, I want an email confirming what actually happened when I register — clearly saying "you're on the waitlist for X" rather than implying I have a confirmed ticket, when that's the case — so that I'm not confused about whether I'm actually attending.
15. As an attendee, I want that same accurate distinction (confirmed vs. waitlisted) whenever my ticket selection changes later, not just at initial signup, so that every status-changing action keeps me correctly informed.
16. As an attendee, I want to actually receive an email when I reach the front of a waitlist and a coupon is issued to me, so that I don't miss my chance to claim a spot (today this email never arrives).
17. As an attendee, I want an email telling me my waitlist offer expired if I don't claim it in time, so that I understand why I lost my spot rather than being left wondering.
18. As an attendee whose registration is cancelled while I'm purely on a waitlist, I want cancellation wording that reflects that I never held a confirmed ticket (not "sorry you can't make it"), so that the message matches my actual situation.
19. As an attendee trying to redeem a promotion coupon, I want redemption to fail if my final ticket selection no longer includes the ticket type the coupon was for, so that I can't accidentally consume a coupon meant for something I've since decided against.
20. As an attendee who reaches out again after previously only joining a waitlist, I want the system to recognize I already have a registration (rather than letting me start a second, separate signup), so that I don't end up with duplicate records.

## Implementation Decisions

**Registration status**
- `RegistrationStatus` gains a third value, `Waitlisted`, alongside the existing `Registered` and `Cancelled`.
- A Registration's status is derived and stored on every ticket-composition change (creation, update, coupon redemption): zero confirmed tickets → `Waitlisted`; one or more confirmed tickets → `Registered`. `Cancelled` remains a separate terminal state reachable from either.
- `CheckIn()` and `Reconfirm()` both reject a `Waitlisted` registration, using the same error pattern already used for the existing `Cancelled` guard on each.
- The reconfirmation-batch selection query already filters explicitly on `Status == Registered`; this automatically and correctly excludes `Waitlisted` registrations with no further change needed.
- Cancelling a registration also withdraws every one of that registration's email's active waitlist entries across all ticket types for the event, so a cancelled attendee can no longer be promoted afterward.

**Self-service create-registration**
- No longer special-cases a waitlist-only submission: a Registration is always created (in `Waitlisted` status when it holds no confirmed tickets), rather than only creating bare waitlist entries with no registration record.

**Self-service update-registration**
- The request shape is extended from a single flat ticket-type-id list to the same split shape used by create (a confirmed-registration list and a waitlist-join list), reusing create's exact registerable/waitlistable/unavailable ticket-state classification and the same ticket-state-conflict error contract, so both endpoints present one consistent mental model to callers.
- The handler mutates the ticket catalog and the relevant per-ticket-type waitlist aggregate(s) together in one transaction owned by the endpoint, consistent with this module's existing transaction-boundary convention.
- Moving a confirmed ticket type to waitlisted, or a waitlisted one to confirmed, is validated against the same live ticket-state classification used at create time (an attendee can only waitlist a ticket type that's genuinely in waitlist mode right now, and can only confirm one that's genuinely registerable right now), with the same conflict-and-retry behavior as create when state has shifted since page load.

**Registration read model**
- Fetching a registration's details is extended to also return, for each ticket type the registration's email currently holds an active waitlist entry for: the ticket type id and the entry's current queue position. This is resolved by looking up the registration's email against active waitlist entries for the event, since a waitlist entry is keyed by (ticket type, email) independently of any registration id.

**Unified coupon model**
- `Coupon`'s `Source` (`Organiser` / `Waitlist`) becomes purely a creation-time label controlling which transactional email intent fires (the coupon-invitation intent for organiser-issued, the waitlist-offer intent for waitlist-issued) — it no longer drives any behavioral branch at redemption time.
- The self-service update-registration coupon-redemption path drops its existing hard requirement that the supplied coupon be waitlist-sourced; any coupon, of either source, can be redeemed through update, since every waitlisted attendee now has a live (non-cancelled) registration to update against.
- Coupon redemption is generalized to support a coupon that allows more than one ticket type. Redemption is partial-tolerant: the final ticket selection needs only overlap the coupon's allowed ticket types by at least one to succeed; any of the coupon's other allowed ticket types not included in that final selection are simply forfeited, and the coupon is still marked fully redeemed (single-use, as today).
- At redemption time, for every ticket type actually granted by the coupon in that submission (the overlap, not the full allow-list), if the redeeming email has an active waitlist entry for that ticket type, it is removed — uniformly, regardless of the coupon's source. This generalizes today's redemption-time cleanup (which is currently gated to waitlist-sourced coupons only and assumes exactly one ticket type).

**Manual VIP promotion (new admin capability)**
- A new admin action, available from the existing per-ticket-type waitlist detail view, lets a team member pick one specific active waitlist entry (identified by its position and masked email, consistent with how that view already presents entries — full identity is available, when needed, via the existing registration-detail drill-down, since every waitlist entrant now has a registration) and issue it a `Source = Waitlist` coupon out of normal first-in-line order.
- This reuses the same claim-window/quiet-hours expiry calculation already used for automatic front-of-queue promotion.
- Issuing this coupon removes the chosen entry from the active waitlist queue immediately (at issuance, not at redemption) — matching how automatic promotion already behaves — and renumbers remaining queue positions accordingly.
- Capacity is not separately re-verified at issuance (consistent with how every coupon redemption in this system already bypasses the normal capacity ceiling); this is a deliberate, existing convention this feature does not change.

**Transactional email fixes and additions**
- Fixed: the automatic front-of-queue promotion path currently issues a coupon but never raises the domain event that triggers its own already-built, already-wired waitlist-offer email — this wiring is restored so promoted attendees are actually notified.
- The registration-confirmation transactional email intent, and the ticket-changed transactional email intent, both become status-aware: they describe confirmed and waitlisted ticket types as clearly separate sections (no "your ticket is confirmed"/QR-code language for a ticket type that's actually waitlisted). The ticket-changed intent now fires whenever *either* the confirmed selection *or* the waitlisted selection changes (today it only fires when the confirmed selection changes, so a pure waitlist-to-waitlist switch currently sends no email at all).
- The cancellation transactional email gets distinct wording for cancelling a purely `Waitlisted` registration (which never held a confirmed ticket), versus the existing wording for cancelling a `Registered` one.
- New: a "your waitlist offer has expired" transactional email is added, sent to the attendee when their issued coupon lapses unclaimed and the queue advances past them.
- A manually-issued VIP coupon uses the exact same waitlist-offer email content as an automatically-issued one.

**Removed**
- The standalone, self-service, email-only join/leave waitlist endpoints (and their handlers/requests/validators) are removed — confirmed unused by any real caller (no test coverage, no hand-written Admin UI caller, and this Admitto deployment currently has no tenant/team besides the one requesting this change), made fully redundant now that joining/leaving a waitlist happens exclusively through ticket-type selection on create/update.
- Their generated Admin UI SDK client functions are dropped as part of the standard SDK-regeneration workflow this repo already requires whenever a backend contract changes.

**Admin UI**
- The new `Waitlisted` registration status gets full display treatment wherever `Registered`/`Cancelled` are already handled: a status badge, a list filter option, and an export column.
- A "promote to VIP coupon" action is added per entry row on the existing waitlist-detail view.

## Testing Decisions

This repo already has an established, three-layer testing convention (documented in `tests/AGENTS.md` and the architecture quality-requirements doc), which this work should follow rather than replace:

- **Domain tests** (aggregate/value-object behavior in isolation) are the right seam for: the new `Waitlisted` status derivation and its `CheckIn`/`Reconfirm` guards on the `Registration` aggregate; the `Waitlist` aggregate's new "issue coupon to a specific entry" behavior (as a refactor alongside its existing "issue to next" behavior) and its immediate queue-removal/renumbering side effect; and `Coupon`'s generalized, partial-tolerant, multi-ticket-type redemption rule.
- **Integration tests** (handler-level, real database) are the *highest* seam for almost everything else, and should be preferred over API-level tests wherever the behavior doesn't specifically concern HTTP routing/auth: the self-service create handler always producing a registration for waitlist-only submissions; the self-service update handler's new split ticket-list request, its ticket-state-conflict behavior on a stale submission, its coupon-redemption path (now source-agnostic, multi-ticket-type, partial-tolerant, with the generalized waitlist-entry cleanup), and the cancellation handler's cascade into withdrawing active waitlist entries; the registration-details read model's new waitlisted-ticket-types-with-position field; the automatic promotion job now actually triggering its email; and the new manual-VIP-promotion handler.
- **API-level tests** (full HTTP pipeline) are the right seam specifically for the genuinely new admin route (manual VIP promotion) and for confirming the update-registration route's request/response contract shape actually matches the new split request — mirroring how the existing update flow already has its own dedicated API-level test coverage.

Existing tests to use as direct prior art: the self-service create-registration integration test suite already covers waitlist-only submission, mixed submission, and stale-state conflict scenarios in the old (registration-less) shape — these existing test cases should be updated to assert a `Waitlisted` registration is produced instead of none, and serve as the template/fixture pattern for the equivalent new update-registration test cases. The existing update-registration integration and API-level test suites are the direct template for the new split-request and coupon-redemption test cases.

Every new or touched test follows this repo's existing conventions: `{Method}_{Condition}_{ExpectedOutcome}` naming, a three-line Given/When/Then comment in plain English above each test, builder/fixture helpers rather than inline setup, and error assertions against typed `Errors.*` constants rather than raw string codes.

Architecture tests must be run and pass before any other suite, per this repo's standing convention, since several of these changes touch cross-module event wiring (the fixed promotion email) and module boundaries (the update handler now reaching into the Waitlist aggregate).

## Out of Scope

- The standalone waitlist join/leave endpoints are removed outright, not deprecated — this repo's only tenant has confirmed no dependency on them; a formal deprecation window is not needed.
- Multi-tenant/other-team considerations: this change assumes a single active tenant and does not add any tenant-facing migration or communication plan.
- Any change to how automatic (non-VIP) queue promotion decides how many people to promote or how claim-window/quiet-hours expiry is computed — the VIP path reuses this unchanged.
- A UI/API way for an admin to un-promote or revert a manually-issued VIP coupon beyond the existing generic coupon-revoke capability.
- Retroactively backfilling `Waitlisted` registrations for any waitlist entries that exist today from before this change ships (existing data migration strategy is not addressed here).

## Further Notes

- Because the update-registration handler now needs to mutate both the ticket catalog and one or more `Waitlist` aggregates within a single endpoint-owned transaction, and because "on the waitlist" no longer implies "no registration exists," several places that historically treated "does this email have a registration" and "is this email waitlisted for ticket type X" as mutually exclusive facts now need to treat them as independently-trackable, simultaneously-true facts about the same attendee.
- The unified coupon model is a deliberate simplification made partway through scoping this work — implementers should resist the temptation to re-introduce source-based branching at redemption time; if a genuine behavioral difference between organizer- and waitlist-issued coupons is discovered during implementation, it should be raised as a new decision rather than assumed.
- This spec's frontend counterpart is `BITBASH_SPEC.md`, which depends on the update-registration request-shape change and the registration read-model extension described above being live before its own work can be verified end-to-end.
