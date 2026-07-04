## 1. Projection Model

- [x] 1.1 Add an Email application read model `EventEmailContextView` under `Application/Projections/EventEmailContext/` with the fields required by the specs.
- [x] 1.2 Expose the projection only on the Email read store (`IEmailReadStore`); add EF configuration, indexes, and constraints keyed by `(team_id, ticketed_event_id)`. Aggregates stay on `IEmailWriteStore`.
- [x] 1.3 Generate an Email EF migration for the `event_email_context_view` table using the official migration workflow.
- [x] 1.4 Add reusable query slices (`GetEventEmailRenderingContext`, `GetActiveReconfirmTriggerSpecs`) that validate required rendering fields and tolerate partial out-of-order updates.

## 2. Integration Events And Synchronization

- [x] 2.1 Add Organization integration event publishing for team accent-color changes or a broader team-updated event consumed by Email.
- [x] 2.2 Extend or add Registrations integration events so Email receives event name, website URL, public slug/link inputs, time zone, reconfirm policy snapshot, self-service ticket-type count, and lifecycle state.
- [x] 2.3 Implement a single role-based `EventEmailContextProjector` (`IIntegrationEventHandler<T>` for the Organization/Registrations events) that idempotently upserts projection rows through the read store and re-issues reconfirm triggers for schedule-affecting events.
- [x] 2.4 Add tests for duplicate delivery and out-of-order Organization/Registrations projection updates.

## 3. Transactional Email Composition

- [x] 3.1 Replace transactional email handlers' `GetEventRegistrationSnapshotAsync` calls with Email projection reads plus trigger-payload facts.
- [x] 3.2 Extend cancellation-trigger payloads with attendee name facts, or keep a documented narrow live Registrations read if extending the payload is rejected.
- [x] 3.3 Derive register, cancel, QR-code, and change-ticket links in Email from projection inputs and public-link configuration.
- [x] 3.4 Remove `IOrganizationFacade.GetTeamBrandingAsync`, `TeamBrandingDto`, and the Registrations email-context aggregation code once no callers remain.
- [x] 3.5 Add or update transactional email tests for projected branding, projected links, missing projection context, and no Organization branding facade call.

## 4. Reconfirm Scheduling

- [x] 4.1 Move reconfirm trigger upsert/remove handlers to use Email projection state for policy and time-zone context.
- [x] 4.2 Update reconciliation to rebuild triggers from Email projection rows instead of enumerating active trigger specs from Registrations.
- [x] 4.3 Keep `RequestReconfirmationsJob` candidate evaluation against live Registrations data and add regression tests for that boundary.
- [x] 4.4 Remove now-unused reconfirm scheduling facade methods from `IRegistrationsFacade` after callers move to Email projection reads.

## 5. Bulk Email Rendering

- [x] 5.1 Enrich built-in/system bulk template parameters from the Email event context projection during fan-out.
- [x] 5.2 Preserve attendee-source recipient resolution through live `IRegistrationsFacade.GetRegistrationsAsync` at job resolution time.
- [x] 5.3 Add tests for reconfirm built-in template parameters and custom bulk content remaining job-owned.

## 6. Documentation And Verification

- [x] 6.1 Update arc42 building-block/runtime/cross-cutting docs to describe the Email-owned event context projection and eventual-consistency semantics.
- [x] 6.2 Add or update an ADR if the projection is treated as a durable cross-module architectural decision.
- [x] 6.3 Run architecture tests first: `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 6.4 Run targeted Email, Registrations, Organization integration/domain tests affected by projection events and email composition.
- [x] 6.5 Validate the OpenSpec change and ensure all task/spec artifacts are ready for apply.
