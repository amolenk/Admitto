## Context

The `UseCases/` folder in each module acts as the grouping layer for all application use cases. Folder names have accumulated inconsistencies over time: 10 folders use an `[Entity]Management` suffix (e.g., `TeamManagement`, `CouponManagement`), while others use plain nouns (`Waitlist`, `Registrations`), plural nouns (`BulkEmails`, `EmailTemplates`), or verb phrases (`SendEmail`). One folder (`SendEmail`) is also structurally misplaced: it contains a use case directly rather than being a group of use cases.

This is a cross-cutting structural rename touching ~250 files across 4 modules.

## Goals / Non-Goals

**Goals:**
- Establish one consistent naming convention for use case group folders: **plural nouns of the primary domain concept**
- Correct the structural misplacement of `SendEmail` (use case at group level)
- Consolidate the single-use-case `AttendeeEmails` folder into the new `Emails/` group

**Non-Goals:**
- Renaming individual use case folders (the leaf level, e.g., `CreateTeam`, `GetCoupons`) — only the group-level folders change
- Changing any application logic, endpoints, or contracts
- Renaming module-level folders (Organization, Registrations, Email, Badges)

## Decisions

### D1: Plural nouns as the group folder convention

**Decision**: Use the plural noun of the primary domain entity/concept as the group folder name. Drop the `Management` suffix entirely.

**Rationale**: `Management` is a content-free suffix — it says nothing about what the group contains beyond "things happen to this entity." Plural nouns (`Teams`, `Coupons`, `TicketTypes`) are shorter, match the DDD/REST mental model, and align with the folders that were already named well (`Registrations`, `BulkEmails`, `EmailTemplates`).

**Alternatives considered**:
- Keep `Management` everywhere for consistency — rejected because it's widely considered a naming smell and adds no meaning
- Use `[Entity]UseCases` suffix — rejected as even more verbose

### D2: Wrap `SendEmail` under `Emails/` group

**Decision**: Create an `Emails/` group folder and move the `SendEmail` use case one level deeper, resulting in `Emails/SendEmail/`.

**Rationale**: Every other first-level folder under `UseCases/` is a *group* of use cases. `SendEmail` was the only exception — it contained `SendEmailCommand.cs` and `SendEmailHandler.cs` directly alongside an `EventHandlers/` folder, i.e., it was a use case disguised as a group. The `Emails/` group also absorbs `GetAttendeeEmails` from the now-defunct `AttendeeEmails/` group.

**Alternatives considered**:
- Rename `SendEmail` to a noun like `EmailDelivery` without wrapping — rejected because it would still be the only group containing a single use case's files at the top level rather than sub-folders
- `TransactionalEmails`, `TriggeredEmails`, `EmailSending` as group name — considered but `Emails` is simpler and parallels `BulkEmails`

### D3: Module-by-module execution order

**Decision**: Rename modules in this order: Organization → Registrations → Email → Badges.

**Rationale**: Organization and Registrations are interdependent (both have `TicketedEventManagement`); doing them together would risk confusion. Sequencing by module keeps each batch of changes reviewable in isolation.

## Risks / Trade-offs

- **Missed references** → Use a project-wide search for each old namespace string before committing; run `dotnet build` to surface any remaining compilation errors
- **Merge conflicts** if a feature branch is in flight → Coordinate with any open PRs before merging this change; the rename is mechanical enough to rebase quickly
- **IDE cached state** → OmniSharp / Roslyn may need a restart after the rename to pick up new namespaces

## Migration Plan

For each module:
1. `git mv` each renamed folder
2. Batch-update namespace declarations inside the moved files (find & replace within the folder)
3. Batch-update `using` statements throughout the codebase (global find & replace on old namespace string)
4. `dotnet build` to confirm no compilation errors
5. Run arch tests, then full test suite

No runtime migration is needed — this is a compile-time-only change with no database, API, or configuration impact.
