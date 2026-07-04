## Why

Bulk email currently lets organizers upload a CSV of arbitrary recipients (the "external list" source) and send them custom content. This is effectively a marketing-email channel to addresses that never registered, which puts the sending domain's reputation at risk (spam complaints, blocklisting) and is outside Admitto's purpose. Removing it protects domain reputation and leaves bulk email as a purely attendee-scoped tool — which also lets us collapse the now-single-source model and delete a meaningful chunk of code.

## What Changes

- **BREAKING**: Remove the `ExternalListSource` recipient source (CSV/arbitrary recipient list) from bulk email. Bulk email SHALL only target registered attendees resolved from the Registrations module.
- **BREAKING**: The create/preview HTTP contract no longer accepts an `externalList` source. `source` is no longer a discriminated one-of; it is always an attendee filter.
- Remove the CSV upload UI from the send bulk-email sheet (file input, client-side `parseCsv`, row-limit handling, the attendees-vs-external-list toggle).
- **Refactor / simplify** now that only one source remains:
  - Collapse the polymorphic `BulkEmailJobSource` value object (and its JSON `$type` discriminator) down to the attendee filter it always carries. Delete `ExternalListSource` and `ExternalListItem`.
  - Drop the "exactly one of two sources" validation and the two-job-pattern guidance.
  - Simplify `BulkEmailRecipientResolver` to the single attendee-resolution path (remove the literal-list branch).
- Remove the planned `--external-list @recipients.csv` option from the aspirational CLI bulk-email spec.
- Update ADR-009 and the arc42 runtime/building-block notes to reflect the single-source model.

Note: there are no `BulkEmailJob` rows in the database yet, so no data migration or back-compat handling for historical `external_list` rows is required.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `bulk-email`: Remove the "exactly one of attendee or external list" requirement and the `ExternalListSource` shape; a job's source is always an attendee filter. Update the create/preview endpoint contract to drop the `externalList` source and the both-sources-rejected validation scenario.
- `admin-ui-bulk-emails`: Remove the CSV upload / external-list recipient option from the send sheet; the send sheet targets registered attendees only.
- `cli-admin-parity`: Remove the `--external-list` option from the planned bulk-email CLI commands.

## Impact

- **Email module** (`src/Admitto.Core/Email`):
  - Domain: `ValueObjects/BulkEmailJobSource.cs` (collapse), delete `ValueObjects/ExternalListItem.cs`.
  - Application: `Sending/Bulk/BulkEmailRecipientResolver.cs`, `UseCases/BulkEmails/CreateBulkEmail/AdminApi/*` (source DTO, validator, request), `PreviewBulkEmail/*`.
  - Persistence: `BulkEmailJobEntityConfiguration.cs` source JSON mapping (no data migration needed — table is empty).
- **Admin UI** (`src/Admitto.UI.Admin`): `send-bulk-email-sheet.tsx` (remove CSV logic + source toggle); regenerate SDK after contract change; update proxy route request types if needed.
- **Tests**: update/remove `BulkEmailRecipientResolverTests` external-list cases, `CreateBulkEmail`/`Preview` tests referencing external list, domain tests, and API tests asserting the both-sources validation.
- **Docs**: `docs/adrs/adr-009-bulk-email-design.md`, `docs/arc42/05-building-block-view.md`, `docs/arc42/06-runtime-view.md`.
- **Specs**: `openspec/specs/bulk-email/spec.md`, `openspec/specs/admin-ui-bulk-emails/spec.md`, `openspec/specs/cli-admin-parity/spec.md`.
