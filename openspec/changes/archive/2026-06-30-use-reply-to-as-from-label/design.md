## Context

Application email currently uses deployment-provided SMTP settings for the actual sender address and team-owned reply-to metadata for the `Reply-To` header. The Organization module owns the team reply-to value, publishes it in team integration events, and the Email module stores it in `TeamEmailContextView` for send-time use.

The current SMTP builders use the configured `FromAddress` value as both the MIME `From` address and the display name. That keeps deliverability safe, but attendees see an Admitto address as the visible sender label even when a team has configured a recognizable contact address.

## Goals / Non-Goals

**Goals:**

- Use the projected team reply-to address as the visible MIME `From` display name when present.
- Preserve the configured Admitto-controlled system sender address as the MIME `From` address.
- Preserve the existing `Reply-To` header behavior.
- Apply the same behavior to transactional and bulk SMTP send paths.

**Non-Goals:**

- Do not allow teams to configure SMTP sender credentials or sender domains.
- Do not change the team settings API, Admin UI fields, or persistence schema.
- Do not introduce a separate from-label field.
- Do not alter Keycloak account-action email sender behavior.

## Decisions

### Use reply-to as display name only

The team reply-to address will be used only as the `MailboxAddress` display name for application email when it is available. The mailbox address remains the deployment-provided `Email:System:FromAddress`.

Alternative considered: replace the actual `From` address with the team reply-to address. Rejected because it conflicts with the existing system SMTP sender identity requirement and can break SPF/DMARC alignment for organizer-owned domains.

### Derive the label in the SMTP builders

The SMTP infrastructure can derive the display label from `EffectiveEmailSettings.ReplyToAddress ?? EffectiveEmailSettings.FromAddress` when constructing MIME messages. This keeps the change small and avoids adding another persisted or projected value.

Alternative considered: add `FromDisplayName` to `EffectiveEmailSettings`. This could make tests more explicit, but it introduces another setting for a value that is directly derivable from existing effective settings.

### Keep single-send and bulk-send paths aligned

Both `MailKitEmailSender` and `MailKitBulkSmtpSender` build MIME messages and must apply the same from-label rule. Bulk send opens a session with effective settings, so the session should retain enough settings to build consistent messages for every recipient.

Alternative considered: only change transactional send first. Rejected because bulk email is also application email and should present the same sender identity to attendees.

## Risks / Trade-offs

- **Some email clients may display an email address as the sender name** -> This is intentional for this change; the actual sender address remains Admitto-controlled and reply handling still uses `Reply-To`.
- **Using an email address as display name could be confused with the actual sender address** -> Specs and tests should assert both the display label and the underlying mailbox address.
- **Future separate brand-name sender labels may need another field** -> This proposal deliberately avoids a new field until there is a clear product requirement for one.

## Migration Plan

No data migration is required. Existing teams with reply-to addresses will automatically get the new display-label behavior after deployment. Rollback restores the previous display label behavior without data changes.
