## ADDED Requirements

### Requirement: Reconfirm scheduling uses Email-owned event context

The Email module SHALL use its Email-owned event rendering/scheduling context projection to register, replace, or remove per-event reconfirm Quartz triggers. The projection SHALL be synchronized from Registrations integration events that describe event creation, event archive, time-zone changes, and reconfirm policy changes.

Email SHALL continue to evaluate reconfirm candidates against live Registrations data when a trigger fires.

#### Scenario: Policy change updates projected trigger context

- **WHEN** Registrations publishes a reconfirm-policy-changed integration event with a non-null policy snapshot
- **THEN** Email updates the event context projection and upserts the per-event reconfirm trigger from projected policy and time-zone context

#### Scenario: Time zone change updates scheduling context

- **WHEN** Registrations publishes a time-zone-changed integration event for an event with an active reconfirm policy
- **THEN** Email updates the event context projection and replaces the per-event trigger so future ticks use the new IANA time zone

#### Scenario: Candidate selection remains live

- **WHEN** a reconfirm trigger fires
- **THEN** Email queries Registrations for currently registered, unreconfirmed attendees and does not use the event context projection as an attendee source

#### Scenario: Archived event removes trigger

- **WHEN** Registrations publishes an event-archived integration event
- **THEN** Email marks or removes the active scheduling context for that event and removes the corresponding reconfirm trigger

### Requirement: Reconfirm scheduling reconciliation rebuilds from Email context

On worker startup or scheduling reconciliation, Email SHALL rebuild per-event reconfirm triggers from active Email event context projection rows that have an active reconfirm policy. Reconciliation SHALL NOT require a synchronous enumeration of active reconfirm trigger specs from Registrations.

#### Scenario: Worker restart restores trigger from projection

- **WHEN** the worker starts and the Email projection contains an active event with a reconfirm policy
- **THEN** reconciliation registers the corresponding Quartz trigger from the projection row

#### Scenario: Event without policy is ignored

- **WHEN** the worker starts and the Email projection contains an active event without a reconfirm policy
- **THEN** reconciliation does not register a reconfirm trigger for that event
