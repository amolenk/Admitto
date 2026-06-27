## ADDED Requirements

### Requirement: Attendee email history remains independent of sender configuration source
Admin attendee email history SHALL continue to list email log entries for a registration regardless of whether the email was sent through prior team SMTP settings or the Admitto system sender. The history response SHALL NOT expose SMTP host, SMTP credentials, or sender configuration details.

#### Scenario: History shows system-sent ticket email
- **WHEN** a ticket email sent through the Admitto system sender is logged for Alice's registration
- **THEN** the attendee email history includes that ticket email with its subject, type, status, and timestamps

### Requirement: Ticket email history reflects conditional change-ticket content only through normal email log fields
The attendee email history SHALL remain an email-log listing and SHALL NOT add a dedicated field for whether the original ticket email contained a change-tickets link. That behavior is determined at render time by the ticket email context.

#### Scenario: History does not expose change-ticket flag
- **WHEN** an admin lists emails for a registration whose ticket email included a change-tickets link
- **THEN** the response contains the normal email log fields and no separate `hasChangeTicketsLink` field
