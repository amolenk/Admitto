# 09: Status-aware confirmation & ticket-changed emails

**What to build:** An attendee's registration-confirmation email and ticket-changed email both accurately describe which of their ticket types are actually confirmed versus merely waitlisted, instead of always implying a confirmed ticket. The ticket-changed email also stops missing pure waitlist-to-waitlist switches.

**Blocked by:** 01 (Waitlisted registration status), 04 (Update-registration supports confirmed⟷waitlist switching)

**Status:** ready-for-agent

- [ ] The registration-confirmation email describes confirmed and waitlisted ticket types in clearly separate sections.
- [ ] A registration holding zero confirmed tickets does not receive "your ticket is confirmed" language or a QR code in its confirmation email.
- [ ] The ticket-changed email is extended with the same confirmed/waitlisted distinction.
- [ ] The ticket-changed email now fires whenever either the confirmed ticket selection or the waitlisted ticket selection changes (today it only fires on a confirmed-selection change).
- [ ] A registration ending up with both confirmed and waitlisted ticket types receives one email describing both correctly.
- [ ] Integration tests cover: a waitlist-only signup's confirmation email content, a mixed signup's confirmation email content, and a waitlist-to-waitlist ticket change producing an email where today it would produce none.
