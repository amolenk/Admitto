## Why

Application emails currently use the team's reply-to email address as the visible MIME `From` display name. That makes replies route correctly, but it exposes an email address where recipients should see the organizer identity.

Using the team name as the sender display name makes attendee-facing emails clearer while preserving the Admitto-controlled sender address and the team's `Reply-To` behavior.

## What Changes

- Use the projected team name as the visible MIME `From` display name for application emails.
- Keep the configured Admitto-controlled sender address as the actual SMTP/MIME `From` address.
- Keep the optional team reply-to email address in the `Reply-To` header only.
- Fall back to the configured Admitto sender address as the display name only when required team context is unavailable.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `email-sending`: changes the sender display-name behavior from team reply-to email address to team name.

## Impact

- Affects Email module SMTP message construction for transactional and bulk application emails.
- Affects Email module effective settings resolution because sender display name must come from the Email-owned team context projection.
- Requires updates to `email-sending` specs and tests that currently expect reply-to address as the visible `From` display name.
- Requires architecture documentation updates where it currently documents reply-to as the visible sender label.
