## ADDED Requirements

### Requirement: Bulk built-in template rendering uses Email event context

For built-in or system bulk email types, including reconfirm email, bulk fan-out SHALL enrich per-recipient template parameters with reusable team/event rendering facts from the Email-owned event context projection. The projection SHALL provide event name, event website URL, public links where applicable, team accent color, and other reusable rendering inputs needed by the selected template.

Custom bulk-email jobs SHALL continue to use job-owned subject/body content and SHALL still receive standard branding parameters during rendering.

#### Scenario: Reconfirm recipient receives projected event links

- **WHEN** a reconfirm bulk-email job processes a recipient
- **THEN** the built-in reconfirm template receives event name, event website URL, register link, cancel link, team accent color, and recipient-specific values from the Email projection plus the recipient snapshot

#### Scenario: Attendee source still resolves against Registrations

- **WHEN** a bulk-email job with an attendee source enters `Resolving`
- **THEN** the recipient resolver still calls Registrations for the live matching registration rows before persisting the recipient snapshot

#### Scenario: Custom job content remains job-owned

- **WHEN** a `bulk-custom` job is processed
- **THEN** the worker renders the persisted job-owned subject and body content, using the Email projection only for reusable branding/context parameters and not as the source of custom content
