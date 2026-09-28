# 02: Fix broken automatic promotion email

**What to build:** When an attendee reaches the front of a waitlist and is automatically issued a coupon, they actually receive an email telling them a spot opened up. Today the coupon is issued silently — the email template and delivery plumbing already exist and are already wired to the right event, but the event that would trigger them is never raised.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] When automatic front-of-queue promotion issues a coupon to the next active waitlist entry, the corresponding domain event is raised (today this call is commented out).
- [ ] Raising that event results in the existing waitlist-offer transactional email being composed and prepared for delivery to the promoted attendee's email address.
- [ ] An integration test exercises the full promotion path and asserts an email delivery was prepared with the correct recipient, ticket type, coupon code, and expiry.
- [ ] No other behavior changes: coupon issuance, immediate queue removal, and position renumbering already in place are untouched.
