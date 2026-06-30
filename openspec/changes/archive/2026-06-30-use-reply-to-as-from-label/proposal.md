## Why

Team owners can already configure a reply-to address so attendee replies route to the right inbox, but outbound emails still show the Admitto sender address as the visible sender label. Reusing the configured reply-to value as the visible from label improves attendee recognition while preserving the Admitto-controlled SMTP sender address required for deliverability.

## What Changes

- Use a team's configured reply-to email address as the MIME `From` display name when application emails are sent for that team.
- Keep the configured Admitto-controlled system sender address as the actual MIME `From` address.
- Keep setting the MIME `Reply-To` header to the configured team reply-to address when present.
- Leave emails for teams without a reply-to address using the existing system sender address as the visible sender label.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `email-sending`: Application email sender identity requirements now allow the team reply-to address as the visible `From` display name while preserving the Admitto-controlled sender address.
- `email-settings`: Team-owned reply-to metadata is still not an SMTP sender setting, but it is also used as the visible from label in outbound application email.

## Impact

- Email send infrastructure that builds MIME messages for single and bulk SMTP delivery.
- Effective email settings resolution and tests that assert reply-to behavior.
- OpenSpec specs for email sending and email settings.
- No API contract, persistence schema, or Admin UI form changes are expected.
