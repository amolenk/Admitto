# OpenSpec → Tests-as-Documentation Migration Plan

> **Temporary working document.** This file tracks a multi-session migration and is not
> permanent project documentation. Delete it as the final step of Phase 4 once the
> migration is complete (see checklist below).

## Context

OpenSpec has been the source of requirements truth (58 capabilities, ~1,009 Given/When/Then
scenarios, driven by an 11-skill/9-command propose → apply → sync → archive workflow). In
practice, small changes have skipped the spec cycle, so specs and code have drifted. We're
inverting the priority: treat the existing 828 backend tests as the executable source of
truth, make them read like clear documentation (borrowing Given/When/Then phrasing from the
specs), fix real coverage gaps, then retire OpenSpec and the AGENTS.md instructions that
depend on it.

Each phase is independently approvable — check in before starting a new phase if scope or
priorities need adjusting.

---


## Phase 1 — Test pyramid & coverage gap report ✅ (assessment delivered in chat 2026-08-22)

- [x] Current shape documented: Arch 17 / Domain 256 / Integration 355 / API 200 (828 total)
- [x] Diamond shape explained via fast-fail guard rule (`docs/arc42/08-crosscutting-concepts.md` §8.10) — accepted as intentional, not a gap
- [x] Gaps identified and prioritized:
  - [ ] Admin UI (`src/Admitto.UI.Admin`) has zero tests — backlog, needs its own test-strategy design
  - [x] `BadgeEvent` domain tests added (see decisions log 2026-08-22) — `ApiKey`, `TeamEventCreationRequest`,
        and most `Registrations/Domain/ValueObjects/*` remain, fix incrementally using the Phase 2 convention
  - [x] Badges module integration coverage is thin — closed the handler-level gaps (see decisions log 2026-08-22)
- [ ] Decide execution order for the above gaps (pending user input)

## Phase 2 — Naming & Given/When/Then convention proposal

- [x] Draft small diff to `tests/AGENTS.md` adding a GWT-comment convention above test bodies
- [x] Produce 2-3 worked before/after examples (one Domain test, one Integration test, one API test)
- [x] Get sign-off on the convention before any repo-wide retrofit
- [x] Repo-wide retrofit across all Domain/Integration/API tests (boy-scout rule applied immediately
      instead of deferring — see decisions log 2026-08-22). `ArchTests` excluded: those assert
      codebase-wide structural conventions, not single scenarios, so GWT doesn't fit.

## Phase 3 — OpenSpec retirement

- [ ] Confirm no outstanding in-flight changes remain (depends on Phase 0)
- [ ] Delete `openspec/` (specs + changes — history preserved via git log)
- [ ] Delete `.codex/skills/openspec-*`
- [ ] Delete `.github/skills/openspec-*`
- [ ] Delete `.github/prompts/opsx-*.prompt.md`
- [ ] Confirm replacement for the "propose before building" function:
      `docs/adrs/` for real architectural decisions; normal PR descriptions for everything smaller
- [ ] Confirm `docs/AGENTS.md` and `tests/AGENTS.md` need no OpenSpec-removal changes (verified: no references today)
- [ ] `grep -r openspec` across the repo returns no dangling references

## Phase 4 — AGENTS.md updates

- [ ] Root `AGENTS.md`: replace OpenSpec-reading step in Feature Implementation Checklist with
      "read the existing tests for the affected module/use case first" + GWT-comment requirement
- [ ] `src/AGENTS.md`: replace "Read the Spec First" workflow step similarly
- [ ] `tests/AGENTS.md`: fold in the Phase 2 GWT-comment convention (once agreed)
- [ ] Flag the no-CI gap as a standing risk worth its own ADR/ticket (separate from this initiative)
- [ ] Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`, then full suite
- [ ] Delete this file (`OPENSPEC_TO_TESTS_PLAN.md`) — migration complete

---

## Decisions log

_Record notable decisions/deviations here as we go, with date._

- 2026-08-22: Plan approved. Diamond-shaped test distribution accepted as intentional
  (fast-fail guard rule), not something to reshape.
- 2026-08-22: Convention signed off, then immediately retrofitted repo-wide (user invoked the
  boy-scout rule) rather than deferring to a separate effort as originally scoped. 140 files /
  ~811 test methods annotated across Domain, Integration, and API test projects via 12 parallel
  agents. `ArchTests` (5 files, 17 tests) intentionally excluded — see Phase 2 notes. Verified:
  all three test projects build clean; Arch (17/17), Domain (272/272), and Integration (355/355)
  suites pass; API suite (200 tests) hit an unrelated environment SSL/UntrustedRoot error talking
  to the Keycloak container in this sandbox — diff confirmed comment-only (600 insertions/0
  deletions across 41 files), so not a regression from this change.
- 2026-08-22: Follow-up cleanup of three items surfaced by the retrofit: (1) removed/rewrote 53
  stale `SC-XXX` scenario-ID comments across 28 files (violated the existing tests/AGENTS.md rule
  against referencing scenario IDs in comments) — deleted where redundant with the new GWT block,
  rewrote to keep non-duplicated context otherwise; (2) renamed
  `BadgesEventLifecycleTests.cs` → `BadgeEventTests.cs` to match its class name `BadgeEventTests`;
  (3) re-verified the one ambiguous GWT case (`SelfRegisterAttendee_StaleWaitlistState_...`)
  against its fixture — confirmed accurate, no change needed. Re-ran Arch/Domain/Integration
  suites after cleanup — all green.
- 2026-08-22: Closed the Badges module integration (handler-level) coverage gap. Before: only
  `BadgeEvents` (create/archive), `RenameBadgeType`, and `UpdateBadgeInstance` had
  `Admitto.Core.IntegrationTests` coverage; `AddBadgeType` had only its cross-team error case.
  Added 10 tests with fixtures across 6 use cases: `AddBadgeType` (happy path),
  `DeleteBadgeType` (cascade-deletes standalone instances, cross-team not-found),
  `GetBadgeTypes` (DB-aggregated instance counts per kind), `AddBadgeInstance` (happy path,
  not-standalone guard), `DeleteBadgeInstance` (happy path, archived-event guard — note this
  handler only checks `EnsureEventActive`, not badge-type kind, unlike its siblings), and
  `GetBadgeInstances` (DB ordering by display name, not-standalone guard). Deliberately left
  `ExportBadgeCsv` without handler-level tests — it's already covered end-to-end at the API
  layer (`Admitto.Api.Tests/Badges/ExportBadgeCsv`) including both CSV branches, empty cases,
  and 404, so a handler-level duplicate would add no signal. Full `Admitto.Core.IntegrationTests`
  suite re-run after the additions: 365/365 passing (up from 355).
- 2026-08-22: Added the missing `BadgeEvent` domain tests (`tests/Admitto.Core.DomainTests/Badges/
  Domain/Entities/BadgeEventTests.cs`, 18 tests). Before this, `BadgeTypeTests.cs` only exercised
  the aggregate incidentally (happy-path `AddBadgeType`/`RenameBadgeType`); the aggregate's own
  invariants — case-insensitive name uniqueness on add/rename (including the self-rename
  exclusion), `EnsureEventActive`/`MarkArchived`, the archived-event guard across
  `AddBadgeType`/`RenameBadgeType`/`DeleteBadgeType`/`EnsureCanManageInstances`, `BadgeTypeNotFound`
  across rename/delete/manage-instances, `DeleteBadgeType` returning the deleted kind, and
  `EnsureCanManageInstances`'s not-standalone guard — were untested. Full `Admitto.Core.DomainTests`
  suite re-run after the additions: 289/289 passing.
