# 04: Update-registration supports confirmed⟷waitlist switching

**What to build:** An attendee editing their existing registration can move a ticket type selection between "confirmed" and "waitlisted" in either direction — a confirmed ticket holder can voluntarily move to a waitlist for something else, and someone on a waitlist can be moved to confirmed if capacity now exists, or move their waitlist entry to a different ticket type.

**Blocked by:** 01 (Waitlisted registration status)

**Status:** ready-for-agent

- [ ] The self-service update-registration request accepts two separate ticket-type-id lists — one for confirmed registration, one for waitlist joining — replacing today's single flat list.
- [ ] The same registerable/waitlistable/unavailable/unknown ticket-state classification used by self-service create is applied to the update request.
- [ ] A request whose classification no longer matches reality at submission time is rejected with the same ticket-state-conflict response shape used by create, including grouped ticket-type-id lists per classification.
- [ ] A successful update mutates the ticket catalog and the relevant per-ticket-type waitlist aggregate(s) together in a single transaction owned by the endpoint.
- [ ] Moving a currently-confirmed ticket type into the waitlist list — when it is genuinely in waitlist mode — releases the catalog claim and adds an active waitlist entry.
- [ ] Moving a currently-waitlisted ticket type into the confirmed list — when it is genuinely registerable — claims catalog capacity and removes the waitlist entry.
- [ ] Registration status (per ticket 01) is recomputed correctly after the update completes.
- [ ] Integration tests cover: confirmed→waitlist, waitlist→confirmed, and a stale-state conflict in each direction.
- [ ] Existing update-registration integration and API-level tests are updated for the new request shape.
