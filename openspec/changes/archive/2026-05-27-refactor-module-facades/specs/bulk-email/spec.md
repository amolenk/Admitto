## MODIFIED Requirements

### Requirement: A bulk-email job has exactly one recipient source — attendee or external list

A `BulkEmailJob.Source` SHALL be exactly one of two discriminated value types: `AttendeeSource` or `ExternalListSource`. There SHALL NOT be a combined / multi-source shape; an organizer who needs to email both registered attendees and an external list SHALL create two separate jobs.

`AttendeeSource` SHALL carry filters consumable by `IRegistrationsFacade.GetRegistrationsAsync`, including at minimum: `TicketTypeSlugs?` (any-of match), `RegistrationStatus?`, `HasReconfirmed?`, `RegisteredAfter?`/`RegisteredBefore?`, and `AdditionalDetailEquals?` (key/value pairs).

`ExternalListSource` SHALL carry an array of `(Email, DisplayName?)` items supplied at request time. There SHALL NOT be a separate persisted "saved recipient list" entity.

#### Scenario: Attendee source resolves against live Registrations data at job start
- **WHEN** a job with `AttendeeSource(ticketTypeSlugs=["workshop-a"])` enters `Resolving`
- **THEN** the resolver calls `IRegistrationsFacade.GetRegistrationsAsync` with the filters and receives one row per matching registration

#### Scenario: External list source needs no facade call
- **WHEN** a job with `ExternalListSource([("alice@x.org","Alice"),("bob@x.org",null)])` enters `Resolving`
- **THEN** the resolver materialises exactly those two recipients without calling the Registrations facade

#### Scenario: Two-job pattern for mixed audiences
- **WHEN** an organizer needs to email both all "workshop-a" attendees and an external invite list
- **THEN** they submit two separate bulk jobs and each carries its own audit record
