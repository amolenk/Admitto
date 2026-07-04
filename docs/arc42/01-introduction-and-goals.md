# 1. Introduction and goals

Organizers of small, free events have few good options for managing registrations. Most ticketing platforms target paid events and charge per-ticket fees, while the free alternatives lack support for opinionated workflows like waiting lists and reconfirmations. Teams end up cobbling together spreadsheets, forms, and manual email — which works until it doesn't.

Admitto is an open-source ticketing system purpose-built for this niche. It handles organizer workflows (teams, event setup, attendee operations) and attendee-facing registration flows, with room to support opinionated registration policies as the project evolves.

## 1.1 Requirements overview

The most important requirements:

- Organizers can create teams, invite members, and manage events.
- Attendees can register for events with capacity-aware ticket allocation.
- The system enforces capacity limits reliably under concurrent usage.
- Modules can evolve independently without service sprawl.

Explicit non-goals:

- Paid ticketing or payment processing.
- Large-scale event management (thousands of concurrent registrations).

## 1.2 Quality goals

| Priority | Quality | Scenario (short) | Acceptance criteria |
| -------: | :------ | :---------------- | :------------------ |
| 1 | Maintainability | A developer adds a new use case to an existing module | Change is contained within the module's projects; no cross-module code changes needed |
| 2 | Reliability | Two attendees register for the last ticket simultaneously | Exactly one registration succeeds; the other gets a clear conflict response |
| 3 | Operational simplicity | An operator deploys a new version | Single build artifact per host; no service mesh or discovery required |
| 4 | Security | An unauthenticated user hits an admin endpoint | Request is rejected with 401 before any business logic runs |

## 1.3 Stakeholders

| Stakeholder | Expectations |
| :---------- | :----------- |
| Attendees | Simple, reliable registration experience |
| Organizers and team members | Low-effort event setup and attendee management |
| Operators | Easy deployment; clear health and diagnostic signals |
| Developers | Understandable module boundaries; fast feedback from tests |

