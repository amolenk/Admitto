## 1. Registrations API And Domain

- [x] 1.1 Add `TimeZone` to `UpdateTicketedEventDetailsHttpRequest`, `UpdateTicketedEventDetailsCommand`, and `UpdateTicketedEventDetailsValidator`.
- [x] 1.2 Update `UpdateTicketedEventDetailsHandler` to parse `TimeZoneId` and pass it into `TicketedEvent.UpdateDetails`.
- [x] 1.3 Fold time-zone assignment into `TicketedEvent.UpdateDetails` while preserving active-event, date-order, policy-window, and optimistic-concurrency behavior.
- [x] 1.4 Remove `TicketedEvent.ChangeTimeZone`, the `UpdateTicketedEventTimeZone` use-case folder, and `MapUpdateTicketedEventTimeZone` endpoint registration.

## 2. Integration Events And Email Projection

- [x] 2.1 Add time zone to `TicketedEventDetailsChangedDomainEvent` and `TicketedEventDetailsChangedIntegrationEvent`.
- [x] 2.2 Update `RegistrationsIntegrationEventPublisher` to publish time zone through `TicketedEventDetailsChangedIntegrationEvent`.
- [x] 2.3 Delete `TicketedEventTimeZoneChangedDomainEvent` and `TicketedEventTimeZoneChangedIntegrationEvent` and remove all handler/interface references.
- [x] 2.4 Update `EventEmailContextView.UpdateDetails` to persist `TimeZone` and report whether the versioned update applied.
- [x] 2.5 Update `EventEmailContextProjector` so applied details-changed events update the projected time zone and reissue reconfirm scheduling from projection state.

## 3. Admin UI And SDK

- [x] 3.1 Update the General settings form to include `timeZone` in the single details update request and remove the separate time-zone save/version increment logic.
- [x] 3.2 Remove the Admin UI BFF `time-zone` proxy route.
- [x] 3.3 Regenerate the Admin UI SDK from the updated OpenAPI spec using the Aspire-backed workflow.
- [x] 3.4 Update generated-SDK imports/usages so only the details-update API is used for event details and time zone.

## 4. Tests

- [x] 4.1 Update Registrations details-update integration tests to pass and assert persisted `TimeZone`.
- [x] 4.2 Update Registrations integration-event publisher tests so details-changed asserts `TimeZone` and the time-zone-changed test is removed.
- [x] 4.3 Update Email projection tests for details-changed events carrying `TimeZone`, including reconfirm rescheduling when the projected time zone changes.
- [x] 4.4 Update or remove API/UI tests that reference the dedicated time-zone endpoint.

## 5. Documentation

- [x] 5.1 Update `docs/arc42/05-building-block-view.md` to remove `TicketedEventTimeZoneChanged` as a separate Registrations integration event.
- [x] 5.2 Update `docs/arc42/06-runtime-view.md` reconfirm scheduling flow to name `TicketedEventDetailsChanged` instead of `TimeZoneChanged`.
- [x] 5.3 Update `docs/arc42/08-crosscutting-concepts.md` Email projection wording so Registrations details events include time zone.
- [x] 5.4 Update `docs/adrs/adr-009-bulk-email-design.md` to stop describing a distinct time-zone-changed integration event.

## 6. Verification

- [x] 6.1 Run architecture tests first: `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 6.2 Run targeted Registrations integration tests for event details and integration-event publishing.
- [x] 6.3 Run targeted Email integration tests for event context projection and reconfirm scheduling.
- [x] 6.4 Run relevant Admin UI validation after SDK regeneration, such as typecheck/lint/build based on available package scripts.
- [x] 6.5 Validate the OpenSpec change status and ensure all artifacts are complete.
