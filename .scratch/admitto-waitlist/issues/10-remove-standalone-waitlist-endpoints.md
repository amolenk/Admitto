# 10: Remove standalone join/leave waitlist endpoints

**What to build:** The standalone, email-only self-service waitlist join/leave endpoints are removed. They're a left-over from before the registration endpoints supported waitlisting directly, are unused by any real caller, and their continued existence is pure surface area to maintain.

**Blocked by:** 04 (Update-registration supports confirmed⟷waitlist switching)

**Status:** ready-for-agent

- [ ] The standalone self-service waitlist join endpoint and its handler/request/validator are removed.
- [ ] The standalone self-service waitlist leave endpoint and its handler/request/validator are removed.
- [ ] Their route registrations are removed from the module's endpoint wiring.
- [ ] The Admin UI SDK is regenerated so it no longer exports client functions for the removed endpoints.
- [ ] Existing test suites pass with no remaining references to the removed use cases.
