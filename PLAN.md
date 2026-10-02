# Waitlist "Offered" state — remaining work

## Context / decisions already agreed (see conversation history)

A waitlisted registration whose only selection is an outstanding waitlist offer must stop
showing as an empty `Waitlisted` registration with nothing in it. Agreed design:

1. `Waitlist` stays the authoritative aggregate (rejected moving entries into `Registration`).
2. A `WaitlistEntry` gets a new status `Offered` (between `Active` and `Removed`). It is set when
   a coupon is issued (automatic or VIP), instead of immediately marking the entry `Removed`.
   `Offered` entries are excluded from queue position/count but still count as a "current
   selection" for duplicate-join checks and for partner updates.
3. `WaitlistEntry.CouponId` (nullable) links an `Offered` entry to the `WaitlistCoupon` record
   tracked on the waitlist. `WaitlistCoupon` itself is unchanged (still the coupon-lifecycle
   record: Issued/Redeemed/Expired + ExpiresAt/Origin).
4. Redemption, expiry, or withdrawal of the offer removes the entry (`Offered -> Removed`).
5. Partner registration details response gets a new `offeredTicketTypes: [{ticketTypeId,
   expiresAt}]` field, separate from `waitlistedTicketTypes` (which keeps queue position).
6. Partner registration **update** treats an `Offered` entry as a current selection: kept if
   still present in the submitted waitlist list, claimed via coupon if moved to the register
   list, withdrawn (coupon silently expired, no email) if omitted entirely.
7. Whenever a `Waitlisted` registration (no confirmed tickets) loses its last selection — last
   active queue entry removed, or last outstanding offer withdrawn/expired — it must be
   auto-cancelled:
   - Final-offer **expiry** (`ProcessExpiredWaitlistCouponsJob`) → new `CancellationReason.WaitlistOfferExpired`,
     which sends **only** the existing "waitlist offer expired" email (no separate cancellation
     email — `RegistrationCancelledIntegrationEventHandler` already no-ops for reasons it doesn't
     recognize, same as `TicketTypesRemoved`).
   - Organizer-driven removal (explicit `RemoveWaitlistEntry`, waitlist `Disable`) → existing
     `CancellationReason.TicketTypesRemoved` (no email, organizer already knows).
8. Attendee-initiated cancellation (`Registration.Cancel`) must also withdraw any outstanding
   offer the registration holds (not just active queue entries), releasing an automatic offer's
   held seat, without sending the "offer expired" email (attendee already got a cancellation
   email).

A shared helper was added for the "cancel if nothing left" check:
`RegistrationCouponHelpers.CancelIfExhausted(registration, eventWaitlists, reason)` — cancels iff
`registration.Status == Waitlisted` and no waitlist in `eventWaitlists` has an Active or Offered
entry for that email. Also added `HasOutstandingWaitlistSelection(waitlists, email)`.

## Already implemented and green (do not redo)

- `Admitto.Core.DomainTests` — **all passing** (67 WaitlistTests incl. 11 new ones for Offered
  lifecycle, 42 RegistrationTests incl. 1 new one for the new cancellation reason).
- Domain layer (`src/Admitto.Core/Registrations/Domain/...`):
  - `WaitlistEntryStatus.Offered` added.
  - `CancellationReason.WaitlistOfferExpired` added.
  - `WaitlistEntry.CouponId` (nullable `CouponId?`) + `internal void Offer(CouponId)`.
  - `Waitlist.cs` rewritten:
    - `AddEntry` now dedupes on `Status != Removed` (so an `Offered` email can't re-join).
    - `HasOfferedEntry(email)` / `GetOfferedEntry(email) -> (CouponId, DateTimeOffset)?` added.
    - `RemoveEntry(EmailAddress, TicketCatalog) -> CouponId?` and
      `RemoveEntry(WaitlistEntryId, TicketCatalog) -> CouponId?` (changed return type from
      `void`) — branch on entry status via shared `RemoveEntryCore`: `Active` leaves the queue +
      renumbers; `Offered` expires its `WaitlistCoupon` + releases an automatic hold; either way
      raises `WaitlistEntryRemovedDomainEvent` and returns the withdrawn coupon id (if any) so
      callers can also expire the real `Coupon` aggregate.
    - `IssueCoupon` (private, backs both `IssueNextCoupon`/`IssueCouponToEntry`) now calls
      `entry.Offer(coupon.Id)` instead of fully removing the entry.
    - `ApplyCouponRedemption` now matches `Active` **or** `Offered` entries (not just Active), and
      only decrements the catalog queue count for the `Active` case (an `Offered` entry already
      left the queue at issuance).
    - `ExpireCoupon` now also removes the matching `Offered` entry (via new private
      `RemoveEntryForCoupon`) and returns the removed `WaitlistEntry?` (changed return type from
      `void`) so callers (the expiry job) can see `entry.RegistrationId`/`Email`.
    - `WithdrawCoupon` likewise removes the matching `Offered` entry via `RemoveEntryForCoupon`.
  - EF config (`WaitlistEntityConfiguration.cs`): added `coupon_id` JSON property for
    `WaitlistEntry.CouponId` with `CouponId.EfCoreValueConverter`. **No migration was added** —
    verify whether one is needed (JSON-column-only changes have historically been snapshot-only
    per existing migrations like `AddWaitlistEntryRegistrationId`; check if EF model snapshot
    needs regenerating / whether a migration is required for CI).
- Email (`RegistrationCancelledIntegrationEventHandler`): no code change was needed —
  `WaitlistOfferExpired` already falls into the existing `_ => null` default case. Added a test
  proving it (`HandleAsync_WaitlistOfferExpired_SilentlySkipsCancellationEmail`), green.
- Partner DTO (`GetPartnerRegistrationDetails`):
  - `PartnerRegistrationDetailDto` gained `OfferedTicketTypes: IReadOnlyList<PartnerOfferedTicketTypeDto>`.
  - New `PartnerOfferedTicketTypeDto(Guid TicketTypeId, DateTimeOffset ExpiresAt)`.
  - Handler populates it via `waitlists.Select(w => (w.Id.Value, w.GetOfferedEntry(email)))`.
  - Tests green (6/6), including new `GetPartnerRegistrationDetails_OutstandingOffer_ReturnsOfferedTicketType`.
  - Fixture (`GetRegistrationDetailsFixture`) gained `WithOfferedTicketType()` + `OfferedTicketTypeId`/`OfferExpiresAt`.
- `RegisterAttendeeHandler`: simplified — `outstandingOfferTicketTypeIds` now computed via
  `waitlists.Where(w => w.HasOfferedEntry(email))` instead of a separate DB query
  (`GetOutstandingOfferTicketTypeIdsAsync` removed). Tests green (73/73).
- `AdminRegisterAttendeeHandler.LeaveWaitlistsAsync`: simplified to use the new
  `waitlist.RemoveEntry(email, catalog) -> CouponId?` return value directly, instead of a second
  query for issued coupons by email. Tests green (27/27 across AdminRegisterAttendee +
  ClaimWaitlistOffer suites).
- `WithdrawWaitlistEntriesHandler` (fires on `RegistrationCancelledDomainEvent`): now withdraws
  **both** `Active` and `Offered` entries (`w.HasActiveEntry(email) || w.HasOfferedEntry(email)`),
  expiring the real `Coupon` for any withdrawn offer. Tests green (5/5, including new
  `WithdrawWaitlistEntries_OutstandingAutomaticOffer_WithdrawsOfferAndReleasesHold`). Full
  `CancelRegistration` suite re-run green (17/17).
- `RegistrationCouponHelpers`: added `HasOutstandingWaitlistSelection` and `CancelIfExhausted`
  (see above).
- `ProcessExpiredWaitlistCouponsJob`: now collects `affectedRegistrationIds` from
  `waitlist.ExpireCoupon(...)`'s returned entry, then — for each affected registration — loads
  **all** of the event's waitlists (not just the ones with a lapsed coupon) and calls
  `RegistrationCouponHelpers.CancelIfExhausted(registration, allEventWaitlists, CancellationReason.WaitlistOfferExpired)`.
  Tests green (15/15, including 2 new ones:
  `Execute_WhenLastSelectionExpires_CancelsRegistration`,
  `Execute_WhenAnotherSelectionRemains_DoesNotCancelRegistration`). Fixture
  (`ProcessExpiredWaitlistCouponsJobFixture`) gained `WithSoleSelectionOneEntryOnePendingCoupon()`,
  `MatchingRegistrationId`, `RegistrationEmail`, and
  `AddOtherWaitlistEntryForMatchingRegistrationAsync(...)`.

All of the above have been verified to build and pass with the real Aspire/Postgres integration
test harness (container runtime is available in this environment — just run
`dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "..."`).

## Not yet implemented — pick up here

### 1. `RemoveWaitlistEntryHandler` (admin explicit entry removal) — Q6 — DONE

Implemented: the handler now captures the entry's `RegistrationId` before removal, expires the
withdrawn coupon (if any) via `writeStore.Coupons`, then loads all of the event's waitlists and
calls `RegistrationCouponHelpers.CancelIfExhausted(registration, allEventWaitlists,
CancellationReason.TicketTypesRemoved)`. Both `RemoveWaitlistEntryTests` green, and
`WaitlistQueuedCountTests` (7/7) re-verified with no regressions.

<details>
<summary>Original task description (for reference)</summary>

A **new test file already exists and is RED** (confirms the gap):
`tests/Admitto.Core.IntegrationTests/Registrations/Application/UseCases/Waitlists/RemoveWaitlistEntry/RemoveWaitlistEntryTests.cs`
with two tests:
- `RemoveWaitlistEntry_LastSelectionForRegistration_CancelsRegistration` (currently fails:
  registration stays `Waitlisted` instead of `Cancelled`).
- `RemoveWaitlistEntry_AnotherSelectionRemains_DoesNotCancelRegistration` (currently passes
  incidentally — verify it still passes after the fix).

Implementation needed in
`src/Admitto.Core/Registrations/Application/UseCases/Waitlists/RemoveWaitlistEntry/RemoveWaitlistEntryHandler.cs`:
- Currently calls `waitlist.RemoveEntry(entryId, catalog)` and ignores the return value (now
  `CouponId?`). Capture it; if non-null, load the real `Coupon` from `writeStore.Coupons` and
  call `.Expire(now)` on it (needs `TimeProvider` injected, or use `DateTimeOffset.UtcNow`; check
  existing convention — other handlers inject `TimeProvider timeProvider`).
  **Note**: the entry being explicitly removed by an admin is normally `Active` (that's the
  existing admin waitlist-page "Remove" action), but it's defensive/correct to also handle
  `Offered` consistently with the other removal paths — just don't forget to expire the coupon
  when a `CouponId` comes back.
- After removal, load the owning `Registration` (need `TeamId`/`EventId`/`Email` — the removed
  entry carries `Email`; you need the entry's email *before* `RemoveEntry` marks it `Removed`, or
  capture email/registrationId from the entry before calling the mutator — check what
  `Waitlist.RemoveEntry(WaitlistEntryId, TicketCatalog)` exposes; you may need to read
  `waitlist.Entries.First(e => e.Id == entryId)` for `Email`/`RegistrationId` *before* calling
  `RemoveEntry`, since the entry itself still exists afterward (status flips to `Removed`, it's
  not deleted from the collection) — so you can actually read `entry.Email`/`entry.RegistrationId`
  from the **same entry object** after the call too, no need to capture early. Confirm via
  `Waitlist.Entries` still containing the entry post-removal (yes, per domain test
  `RemoveEntry_ByEntryId_WhenEntryAlreadyRemoved_IsIdempotent`).
- Then: query **all** waitlists for `(eventId, teamId)` (not just this one), find the
  `Registration` by `(eventId, teamId, email)`, and call
  `RegistrationCouponHelpers.CancelIfExhausted(registration, allEventWaitlists, CancellationReason.TicketTypesRemoved)`.
- Mirror the job's pattern: reuse `writeStore.Waitlists.Where(w => w.EventId == eventId && w.TeamId == teamId).ToListAsync(...)` for the exhaustion check (this handler already has `catalog`/`eventId`/`teamId` in scope — check current handler body for exact variable names).
- Run: `dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~RemoveWaitlistEntryTests"` until green.
- Also re-run `WaitlistQueuedCountTests` (`RemoveWaitlistEntry_OrganizerRemovesEntry_CountsLeaveOnCatalog` uses this handler) to make sure nothing broke:
  `dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~WaitlistQueuedCountTests"`.

</details>

### 2. `DisableWaitlistHandler` (explicit waitlist disable) — Q6 — DONE

Implemented: `Waitlist.Disable` now returns `(IReadOnlyList<Coupon> Coupons, IReadOnlyList<WaitlistEntry>
RemovedEntries)` (TDD: new domain test `Disable_RemovesEntries_SurfacesRemovedEntriesForCallers` added
first, then the signature changed, then the three existing `Disable_*` tests' destructuring updated).
`DisableWaitlistHandler` loads all of the event's waitlists plus the owning registrations for each
removed entry's `RegistrationId`, and calls `CancelIfExhausted(..., CancellationReason.TicketTypesRemoved)`
per registration. New `DisableWaitlistTests.cs` (2 tests, mirroring `RemoveWaitlistEntryTests.cs`) green.
`ProcessExpiredWaitlistCouponsJobTests` re-run (15/15, no regression). Also fixed a stale assertion in
`UpdateTicketTypeWaitlistTests.UpdateTicketType_WaitlistDisabled_RemovesAllEntriesAndKeepsOutstandingCoupon`
left over from the earlier `Offered`-status work — it asserted every entry ends up `Removed`, but an
entry holding an outstanding, unclaimed offer correctly stays `Offered` across a disable.

<details>
<summary>Original task description (for reference)</summary>

**No test written yet.** Needs:
- `Waitlist.Disable(...)` currently returns only `IReadOnlyList<Coupon>` (new coupons issued to
  the front of the queue from freed slots). It removes the *remaining* `Active` entries (not
  `Offered` ones — per existing documented behavior, outstanding coupons/offers stay valid across
  a disable). To know which registrations lost their last selection, `Disable` needs to also
  surface the **removed** entries (email/registrationId) — e.g. change its return type to a
  tuple `(IReadOnlyList<Coupon> Coupons, IReadOnlyList<WaitlistEntry> RemovedEntries)`, or add an
  `out`/separate list. Check current signature in
  `src/Admitto.Core/Registrations/Domain/Entities/Waitlist.cs` (`public IReadOnlyList<Coupon> Disable(...)`)
  and its existing domain tests in `WaitlistTests.cs` (`Disable_...` tests) — **those tests assert
  on the return value as `IReadOnlyList<Coupon>` directly** (e.g.
  `coupons.ShouldHaveSingleItem().Email...`), so changing the return shape will require updating
  those assertions too (follow TDD: write a new domain test for the removed-entries list first,
  then change the signature, then fix the existing `Disable_*` tests' destructuring).
- Then in `DisableWaitlistHandler`
  (`src/Admitto.Core/Registrations/Application/UseCases/Waitlists/DisableWaitlist/DisableWaitlistHandler.cs`):
  for each removed entry, load the owning `Registration` and call `CancelIfExhausted` with
  `CancellationReason.TicketTypesRemoved`, same pattern as above (load all event waitlists once,
  reuse across all removed entries/registrations — these removed entries are all on the *same*
  waitlist being disabled, but a given attendee could still hold a selection on a *different*
  ticket type's waitlist, so the exhaustion check must still scan all of the event's waitlists).
- Add integration test(s) in a new
  `tests/Admitto.Core.IntegrationTests/.../Waitlists/DisableWaitlist/DisableWaitlistTests.cs`
  (doesn't exist yet) covering: (a) a waitlisted registration whose only entry is removed by
  disable → cancelled with `TicketTypesRemoved`; (b) a registration that also holds another
  waitlist entry elsewhere → not cancelled. Mirror the style of the new
  `RemoveWaitlistEntryTests.cs`.
- There's an existing fixture at
  `tests/Admitto.Core.IntegrationTests/Registrations/Application/Jobs/ProcessExpiredWaitlistCouponsJobFixture.cs`
  that already calls `DisableWaitlistHandler` (`DisableWaitlistAsync` helper) — re-run
  `ProcessExpiredWaitlistCouponsJobTests` (`Execute_WhenCouponExpiresAfterWaitlistDisabled_...`)
  after this change to make sure nothing regressed.

</details>

### 3. `UpdatePartnerRegistrationHandler` — Q10 (Offered as a current selection on partner update) — DONE

Implemented all 5 points: `currentWaitlistIds` now includes `HasOfferedEntry`; `toWaitlistLeave` excludes
`couponGrantedIds` (regression-tested); the leave loop expires the real `Coupon` for any withdrawn offer
(same pattern as the other handlers); and a `CancelIfExhausted` call was added at the end using
`CancellationReason.LeftWaitlist` (new enum value, distinct from `TicketTypesRemoved` since this path is
attendee self-service) — note the exhaustion check uses `waitlistsById.Values`, not the original `waitlists`
list, since a brand-new waitlist created by the join loop (first entry for a ticket type) only exists in the
former; using `waitlists` regressed `UpdatePartnerRegistration_ConfirmedToWaitlist_ReleasesClaimAndAddsWaitlistEntry`
(falsely cancelled a registration that had just joined a fresh waitlist). `RegistrationCancelledIntegrationEventHandler`
needed no change: `LeftWaitlist` already falls into its `_ => null` default case, same as `TicketTypesRemoved`.
Added 4 new tests to `UpdatePartnerRegistrationHandlerTests.cs` (decline-with-other-selection,
decline-as-last-selection → cancelled, claim-via-coupon-with-other-changes regression, keep-offered-ticket-type)
plus a new fixture factory `WithSoleOutstandingOffer()`. Post-implementation code review (Standards + Spec axes)
flagged duplicated coupon-withdrawal-then-expire logic across handlers; extracted a shared
`RegistrationCouponHelpers.ExpireWithdrawnCouponAsync` helper and applied it to both `UpdatePartnerRegistrationHandler`
and `RemoveWaitlistEntryHandler`. Full `UpdatePartnerRegistration` suite green (21/21),
no regressions in `RegistrationCancelledIntegrationEventHandlerTests`, `GetWaitlistDetails`, `WaitlistQueuedCountTests`
(15/15), and the full `Admitto.Core.IntegrationTests` suite (563/563) and `Admitto.Core.DomainTests` (408/408).
`Admitto.Core.ArchTests` green (17/17). Updated `docs/arc42/06-runtime-view.md` §6.6 to describe `Offered`-aware
diffing, offer decline/withdrawal, and the new auto-cancel.

<details>
<summary>Original task description (for reference)</summary>

**Not started.** This is the most involved remaining piece. File:
`src/Admitto.Core/Registrations/Application/UseCases/Registrations/UpdatePartnerRegistration/UpdatePartnerRegistrationHandler.cs`.

Required changes (see the earlier design discussion in this conversation for full rationale):
1. `currentWaitlistIds` (currently `waitlists.Where(w => w.HasActiveEntry(registration.Email))`)
   must also include `Offered` ticket types:
   `waitlists.Where(w => w.HasActiveEntry(email) || w.HasOfferedEntry(email))`.
2. **Critical bug to avoid**: `toWaitlistLeave = currentWaitlistIds.Except(waitlistSet)` must
   **exclude** `couponGrantedIds` — otherwise, when an attendee claims their offer by moving a
   ticket type from the waitlist list into the register list *with the coupon code*, the
   "leave" loop will run `RemoveEntry` on that ticket type's `Offered` entry *before* the
   redemption code runs later in the method, wrongly expiring the just-redeemed coupon. Fix:
   `var toWaitlistLeave = currentWaitlistIds.Except(waitlistSet).Except(couponGrantedIds).ToList();`
   (`couponGrantedIds` is already computed earlier in the method via `SplitCouponGranted`).
3. The `toWaitlistLeave` loop currently calls `waitlist.RemoveEntry(registration.Email, catalog)`
   and ignores the return value — capture it (`CouponId?`) and expire the real `Coupon` for any
   withdrawn offer (same pattern as the other handlers above). This implements "declining an
   offer by omitting it from the submitted waitlist list" (Q10).
4. At the very end of the handler (after the coupon-redemption `ApplyRedemptionToWaitlistsAsync`
   call), add the exhaustion-cancel check: if the registration is now `Waitlisted` with no
   selection left anywhere, cancel it. Reuse `waitlists` (already loaded for the whole event in
   this handler) with `RegistrationCouponHelpers.CancelIfExhausted(registration, waitlists, CancellationReason.TicketTypesRemoved)`
   — **note**: this path is attendee self-service, not organizer-driven, so reconsider whether
   `TicketTypesRemoved` is the right reason here or whether a self-service "attendee walked away
   from their last offer/queue spot" deserves its own reason; this wasn't explicitly settled in
   the conversation — default to `TicketTypesRemoved` (no email) unless revisited, since the
   attendee initiated the change themselves and doesn't need a notification about it.
   UPDATE: Having a distinct cancel reason here is preferable: something like `LeftWaitlist`
5. Tests to add in
   `tests/Admitto.Core.IntegrationTests/Registrations/Application/UseCases/Registrations/UpdatePartnerRegistration/`
   (existing fixture `UpdatePartnerRegistrationFixture.WithWaitlistCoupon()` already sets up an
   attendee holding an automatic offer — good starting point, but check it still matches the new
   `Offered` semantics since it currently comments "their own entry was removed at issuance" —
   update that comment/assumption):
   - Declining an offer (submit without the ticket type in either list) → offer withdrawn, no
     email, registration not cancelled if it still holds another selection, cancelled if not.
   - Claiming an offer via coupon in the same request that also changes other state → coupon
     redeemed correctly, no accidental double-expiry (regression test for the bug in point 2).
   - Keeping an offer (still included in `waitlistTicketTypeIds`) while changing unrelated
     fields → offer entry untouched, no new entry created, `AddEntry`'s dedup-on-`Offered`
     already covered at the domain level but worth an integration-level check too.

### 4. Open items / things to double-check before calling this done

- **Migration for `WaitlistEntry.CouponId`**: confirm whether `dotnet ef migrations add` is
  needed. Check `src/Admitto.Core/Registrations/Infrastructure/Persistence/Migrations/` for the
  pattern used by `20261002062648_AddWaitlistEntryRegistrationId.cs` (snapshot-only, no SQL) and
  do the same if required — or determine EF doesn't need one at all for owned-JSON property
  additions (migrations may only be needed if the model snapshot is asserted against in tests).
  Run `dotnet ef migrations list` / `dotnet ef migrations add AddWaitlistEntryCouponId` from
  `src/Admitto.Core` if the tooling is set up, or check `tests/Admitto.Core.IntegrationTests` for
  a model-snapshot-matches-migrations guard test.
- **Architecture tests**: per `AGENTS.md`, run
  `dotnet test tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` — **not yet run** in
  this session. Do this early in the next session since it's supposed to run first.
- **Full test suite**: run the complete `Admitto.Core.DomainTests`,
  `Admitto.Core.IntegrationTests`, and `Admitto.Api.Tests` suites once all of the above is done,
  per the `implement` skill's instructions ("single test files regularly, full suite once at the
  end").
- **Admin UI / API surface**: check whether the Admin UI's per-ticket-type waitlist page
  (`GetWaitlistDetailsHandler`, mentioned in `06-runtime-view.md` §6.6) needs to show `Offered`
  entries distinctly too — out of explicit scope per the conversation (only the Partner API DTO
  was agreed on, Q9), but worth a quick check that `Offered` entries don't silently disappear
  from or break the existing admin waitlist view (`GetWaitlistDetailsHandler` reads
  `waitlist.Entries`/`waitlist.Coupons` directly — check it doesn't assume only `Active`/`Removed`
  statuses exist).
- **Documentation**: per repo `AGENTS.md`/`docs/AGENTS.md` conventions, update
  `docs/arc42/06-runtime-view.md` §6.6 (waitlist runtime behavior — several paragraphs there
  describe the *old* "entry removed at issuance" behavior and need updating to describe
  `Offered`) and `docs/arc42/08-crosscutting-concepts.md` §"Waitlist-held capacity" if any
  invariants changed. **Not started.** This is required by the repo's own contribution
  guidelines before considering the feature complete.
- **CONTEXT.md**: consider adding a "Waitlist offer" lifecycle note / "Offered" term if the
  domain-modeling skill conventions in this repo expect it (check `CONTEXT.md` ubiquitous
  language list — currently has "Waitlist offer" but doesn't mention the entry-level `Offered`
  status as a term; optional, use judgement).
- **Code review**: once functionally complete, run `/code-review` as instructed, then commit.
  **No commit has been made yet** — all work so far is uncommitted on branch
  `public-capacity-design`.

## Test commands reference

```bash
# Architecture (run first, not yet run this session)
dotnet test tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj

# Domain (fast, no containers)
dotnet test tests/Admitto.Core.DomainTests/Admitto.Core.DomainTests.csproj

# Targeted integration suites exercised so far (all green):
dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~WaitlistTests"
dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~RegistrationCancelledIntegrationEventHandlerTests"
dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~GetPartnerRegistrationDetailsHandlerTests"
dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~RegisterAttendee"
dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~AdminRegisterAttendee|FullyQualifiedName~ClaimWaitlistOffer"
dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~WithdrawWaitlistEntriesTests"
dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~CancelRegistration"
dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~ProcessExpiredWaitlistCouponsJobTests"

# Currently RED — pick up here:
dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~RemoveWaitlistEntryTests"

# Not yet exercised at all this session — run before/after DisableWaitlist and UpdatePartnerRegistration work:
dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~WaitlistQueuedCountTests"
dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~UpdatePartnerRegistration"
dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj --filter "FullyQualifiedName~GetWaitlistDetails"

# Full suites (once everything above is green)
dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj
dotnet test tests/Admitto.Api.Tests/Admitto.Api.Tests.csproj
```
