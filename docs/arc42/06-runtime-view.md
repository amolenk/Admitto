# 6. Runtime view

## 6.1 Admin command flow (write path)

This is the most important flow — it shows how a write request moves through validation, authorization, command handling, persistence, and outbox dispatch.

```mermaid
sequenceDiagram
  participant Client
  participant Endpoint as API Endpoint
  participant Filter as ValidationFilter
  participant Auth as Authorization
  participant Mediator
  participant Handler as Command Handler
  participant UoW as Module UnitOfWork
  participant DbCtx as Module DbContext
  participant Interceptor as DomainEventsInterceptor
  participant Outbox as OutboxWriter

  Client->>Endpoint: POST /admin/...
  Endpoint->>Filter: FluentValidation on request DTO
  Filter-->>Endpoint: Valid or 400
  Endpoint->>Auth: Policy check (admin / team role)
  Endpoint->>Mediator: Send(command)
  Mediator->>Handler: HandleAsync(command)
  Handler->>DbCtx: Mutate aggregates
  Endpoint->>UoW: SaveChangesAsync()
  UoW->>DbCtx: SaveChanges (triggers interceptor)
  Interceptor->>Mediator: PublishDomainEventAsync (within transaction)
  Interceptor->>Outbox: TryEnqueue mapped module/integration events
  DbCtx-->>UoW: Transaction committed
  UoW->>Outbox: Best-effort dispatch to queue
  Endpoint-->>Client: 200/201
```

Key invariant: the **endpoint** calls `SaveChangesAsync`, not the handler. Handlers mutate state but never commit.

## 6.2 Domain event to outbox flow

Shows how a domain event raised inside an aggregate ends up as a queued message.

```mermaid
sequenceDiagram
  participant Aggregate
  participant Interceptor as DomainEventsInterceptor
  participant Mediator
  participant Policy as IMessagePolicy
  participant Writer as OutboxWriter
  participant Table as OutboxMessages table

  Aggregate->>Aggregate: AddDomainEvent(...)
  Note over Interceptor: Runs during SaveChangesAsync
  Interceptor->>Mediator: PublishDomainEventAsync (sync, in-transaction)
  Interceptor->>Policy: ShouldPublishModuleEvent? ShouldPublishIntegrationEvent?
  Policy-->>Writer: Mapped event payload
  Writer->>Table: INSERT pending outbox message
  Note over Table: Committed in same DB transaction as aggregate changes
```

Message type naming: module events use `{module}.{event-name}` (e.g. `organization.user-created`); integration events use `integration.{module}.{event-name}`.

## 6.3 Cross-module query

Modules never access each other's DbContext. Instead, the consuming module calls a facade defined in the provider's Contracts project.

Example: Registrations module needs ticket types from Organization.

1. `RegisterAttendeeHandler` calls `IOrganizationFacade.GetTicketTypesAsync(eventId)`
2. `OrganizationFacade` dispatches `GetTicketTypesQuery` via `IMediator`
3. Handler queries `OrganizationDbContext` and returns `TicketTypeDto[]`
4. Optional `CachingOrganizationFacade` decorator caches repeated lookups

The same facade is used by authorization handlers to resolve team membership roles.

## 6.4 Event creation (Organization → Registrations async flow)

Event creation is a two-phase async flow. Organization validates team-level invariants and acts as the creation **gatekeeper**; Registrations materialises the authoritative `TicketedEvent` and reports back with an outcome. The Admin UI submits the request and polls a creation-status endpoint until it sees a terminal state.

```mermaid
sequenceDiagram
  participant UI as Admin UI
  participant OrgEp as Organization endpoint
  participant Team as Team aggregate
  participant OrgOutbox as Org outbox
  participant RegHandler as Registrations integration-event handler
  participant RegEvent as TicketedEvent aggregate
  participant Catalog as TicketCatalog
  participant RegOutbox as Reg outbox
  participant OrgHandler as Organization integration-event handler

  UI->>OrgEp: POST /admin/teams/{teamSlug}/events
  OrgEp->>Team: RequestCreation(slug, requester)
  Team->>Team: EnsureNotArchived(); PendingEventCount++
  Team->>Team: Add TeamEventCreationRequest (Pending)
  OrgEp->>OrgOutbox: TicketedEventCreationRequested (CreationRequestId, TeamId, Slug)
  OrgEp-->>UI: 202 Accepted + Location: /admin/teams/{slug}/event-creations/{id}
  OrgOutbox->>RegHandler: deliver
  RegHandler->>RegEvent: insert TicketedEvent (TeamId, Slug, ...)
  alt success
    RegHandler->>Catalog: create Active TicketCatalog
    RegHandler->>RegOutbox: TicketedEventCreated
  else duplicate slug
    RegHandler->>RegOutbox: TicketedEventCreationRejected (reason=duplicate_slug)
  end
  RegOutbox->>OrgHandler: deliver (idempotent on CreationRequestId)
  OrgHandler->>Team: RegisterEventCreated / RegisterEventRejected
  Team->>Team: PendingEventCount--; Active/Rejected counter++
  UI->>OrgEp: GET /admin/teams/{slug}/event-creations/{id} (poll)
  OrgEp-->>UI: { status: Created | Rejected | Pending, link }
```

Key properties:

- Organization owns `PendingEventCount` and the `TeamEventCreationRequest` state; these are mutated in the same unit of work as the outbox write.
- `CreationRequestId` is the idempotency key on every response event. Organization handlers are idempotent on redelivery and also tolerate out-of-order arrival of `TicketedEventCreated` vs the original request's own commit.
- A Quartz job (`ExpireStaleEventCreationRequestsJob`) expires `Pending` requests older than a configurable timeout and rolls back `PendingEventCount`, so team-archive is never blocked indefinitely by lost or unprocessable requests.

## 6.5 Event cancel / archive (Registrations → Organization)

`Cancel` and `Archive` operations target the authoritative `TicketedEvent` aggregate in Registrations. The lifecycle transition is projected atomically onto `TicketCatalog.EventStatus` (via an in-module domain event in the same unit of work), and propagated to Organization as an integration event so the team's counters can be updated.

```mermaid
sequenceDiagram
  participant UI as Admin UI
  participant RegEp as Registrations endpoint
  participant Event as TicketedEvent
  participant Catalog as TicketCatalog
  participant RegOutbox as Reg outbox
  participant OrgHandler as Organization integration-event handler
  participant Team

  UI->>RegEp: POST /admin/.../events/{eventSlug}/cancel (or /archive)
  RegEp->>Event: Cancel() / Archive()
  Event-->>Event: raises TicketedEventStatusChanged (in-module)
  Event->>Catalog: project EventStatus (same UoW)
  RegEp->>RegOutbox: TicketedEventCancelled / TicketedEventArchived (same UoW)
  RegOutbox->>OrgHandler: deliver (idempotent on TicketedEventId + transition)
  OrgHandler->>Team: RegisterEventCancelled / RegisterEventArchived
  Team->>Team: ActiveEventCount-- ; CancelledEventCount++ (or Archived++)
```

Because `TicketCatalog.EventStatus` is updated in the same transaction as `TicketedEvent.Cancel/Archive`, any in-flight registration that has already loaded `TicketCatalog` at a prior version fails its claim with a `DbUpdateConcurrencyException` — no registration can slip past a lifecycle transition.

## 6.6 Attendee registration (atomic status + capacity gate)

The registration handler (self-service or coupon) loads both `TicketedEvent` (for window / domain / active-status policy checks) and `TicketCatalog` (for the atomic claim) in the same unit of work.

```mermaid
sequenceDiagram
  participant Endpoint as Public endpoint
  participant Handler
  participant Event as TicketedEvent
  participant Catalog as TicketCatalog

  Endpoint->>Handler: Send(RegisterCommand)
  Handler->>Event: load (policy invariants: window, domain, status)
  Handler->>Catalog: load
  Handler->>Catalog: Claim(...)  // atomic on EventStatus + capacity
  Note over Catalog: Refuses when EventStatus != Active (mapped to "event not active")
  Endpoint->>Endpoint: SaveChangesAsync (UoW)
```

Coupons bypass capacity / window / domain checks but do not bypass the active-status gate.

## 6.7 Policy mutation flow

Policy commands (`ConfigureRegistrationPolicyCommand`, `ConfigureCancellationPolicyCommand`, `ConfigureReconfirmPolicyCommand`) load the `TicketedEvent` aggregate and call the matching policy mutator directly. Each mutator refuses when the event's status is not Active, so there is no separate lifecycle guard. Optimistic concurrency is supplied by `TicketedEvent.Version`.

```mermaid
sequenceDiagram
  participant Endpoint as Admin endpoint
  participant Handler as Policy handler
  participant Event as TicketedEvent
  participant UoW as Module UnitOfWork

  Endpoint->>Handler: Send(command, Version)
  Handler->>Event: load with expected Version
  Handler->>Event: ConfigureXxxPolicy(...)
  Note over Event: Throws if Status != Active
  Endpoint->>UoW: SaveChangesAsync
```

## 6.8 Registration-confirmation email flow

When an attendee registers successfully, the API handler emits an `AttendeeRegistered` integration event via the outbox. The Worker picks it up and attempts to send a confirmation email.

```mermaid
sequenceDiagram
    participant Api as API host
    participant Outbox as Integration-event outbox
    participant Worker as Worker host
    participant EmailHandler as AttendeeRegistered handler (Email module)
    participant EmailOutbox as Email outbox
    participant SMTP as SMTP server (MailDev / real)
    participant EmailLog as email.email_log

    Api->>Outbox: AttendeeRegistered (in same UoW transaction)
    Worker->>Outbox: poll & dequeue
    Worker->>EmailHandler: dispatch AttendeeRegistered
    EmailHandler->>EmailLog: check idempotency key (attendee-registered:<registrationId>)
    alt already Sent
        EmailHandler-->>Worker: ack (no-op, idempotency guard)
    else not yet sent
        EmailHandler->>EmailHandler: resolve effective EmailSettings (event → team)
        alt no settings
            EmailHandler->>EmailLog: insert Failed (email not configured)
        else settings found
            EmailHandler->>EmailHandler: resolve EmailTemplate (event → team → built-in)
            EmailHandler->>EmailHandler: render subject + bodies via Scriban
            EmailHandler->>EmailOutbox: enqueue SendEmail command (in same UoW)
            EmailHandler->>EmailLog: insert Pending
            Worker->>EmailOutbox: poll & dequeue SendEmail
            Worker->>SMTP: SMTP send
            Worker->>EmailLog: update Sent (or Failed on SMTP error)
        end
    end
```

**Idempotency**: the `EmailLog` row with key `attendee-registered:<registrationId>` is checked before every send attempt. A re-delivered integration event that already produced a `Sent` log row is acked without a second send.

**Degraded mode**: if no effective email settings exist for the event, a `Failed` log row is written with reason "email not configured" and the integration event is still acked — registration itself is unaffected.

## 6.9 Bulk-email fan-out (single SMTP connection)

When an admin starts a bulk send (or the reconfirm scheduler ticks), a `BulkEmailJob` is created in `Pending` state and a Quartz trigger queues `BulkEmailFanOutJob`. The fan-out job opens **one** SMTP connection per pickup and streams every recipient through it; the single-send pipeline is bypassed deliberately to avoid one TLS handshake per recipient.

```mermaid
sequenceDiagram
    participant Admin as Admin / Reconfirm tick
    participant Endpoint as Admin endpoint / Reconfirm job
    participant Job as BulkEmailJob
    participant FanOut as BulkEmailFanOutJob (Worker)
    participant Resolver as Recipient resolver
    participant Facade as IRegistrationsFacade
    participant SMTP as SMTP server
    participant EmailLog as email.email_log

    Admin->>Endpoint: start bulk send
    Endpoint->>Job: create (Pending) with Source
    Endpoint-->>Admin: 202 Accepted (jobId)
    FanOut->>Job: pick up (DisallowConcurrentExecution per jobId)
    Job->>Job: transition Pending → Resolving
    alt AttendeeSource
      Resolver->>Facade: QueryRegistrationsAsync(eventId, filters)
      Facade-->>Resolver: projection rows
    else ExternalListSource
      Resolver->>Job: read embedded list
    end
    Resolver->>Job: persist frozen Recipients snapshot
    Job->>Job: transition Resolving → Sending
    FanOut->>SMTP: connect (single connection)
    loop for each Pending recipient
      FanOut->>FanOut: check CancellationRequestedAt
      FanOut->>SMTP: MAIL FROM / RCPT TO / DATA
      FanOut->>EmailLog: insert key=bulk:{jobId}:{email} (Sent or Failed)
      FanOut->>Job: update per-recipient status + counters
      FanOut->>FanOut: Task.Delay(PerMessageDelay, ct)
    end
    FanOut->>SMTP: QUIT
    Job->>Job: finalise → Completed / PartiallyFailed / Cancelled / Failed
```

**Resume-after-crash**: only `Pending` rows on the snapshot are picked up on the next run; per-recipient `EmailLog` uniqueness on `(ticketed_event_id, recipient, idempotency_key)` is the second line of defence.

**Cancellation**: `POST /admin/.../bulk-emails/{id}/cancel` sets `CancellationRequestedAt` on the aggregate; the worker observes it between recipients and during the per-message delay, transitions remaining `Pending` rows to `Cancelled`, and closes the SMTP session cleanly.

## 6.10 Reconfirm scheduling (per-event Quartz trigger)

The Email module owns one static Quartz job (`EvaluateReconfirmJob`) and registers a per-event trigger whose cron is derived from `TicketedEventReconfirmPolicy` and evaluated in `TicketedEvent.TimeZone`. Triggers are kept in sync with Registrations through integration events.

```mermaid
sequenceDiagram
    participant RegOutbox as Reg outbox
    participant ReconfirmHandlers as Reconfirm scheduler handlers
    participant Quartz as Quartz scheduler
    participant Eval as EvaluateReconfirmJob (per-event trigger)
    participant Facade as IRegistrationsFacade
    participant Job as BulkEmailJob (reconfirm)
    participant FanOut as BulkEmailFanOutJob

    RegOutbox->>ReconfirmHandlers: TicketedEventCreated / ReconfirmPolicyChanged / TimeZoneChanged / Cancelled / Archived
    ReconfirmHandlers->>Quartz: upsert / remove per-event trigger (cron in event TZ)
    Note over Quartz: fires per cadence inside reconfirm window
    Quartz->>Eval: trigger fires (eventId)
    Eval->>Facade: QueryRegistrationsAsync(Status=Registered, HasReconfirmed=false)
    Facade-->>Eval: candidate projection
    alt no candidates
      Eval-->>Quartz: ack (no-op)
    else candidates present
      Eval->>Job: create BulkEmailJob (email_type=reconfirm, attendee snapshot)
      Job->>FanOut: queued (see §6.9)
    end
```

**Eligibility**: live `HasReconfirmed=false` is the only gate — no extra `email_log` cadence filter. The cron *is* the cadence; tightening the policy (e.g. 7d → 3d) immediately changes prompt frequency.

**Lifecycle cleanup**: `TicketedEventCancelled` and `TicketedEventArchived` integration events remove the trigger so cancelled or archived events stop receiving reconfirm prompts.

## Done-when

- [x] The most important end-to-end flow is documented.
- [x] Each scenario has a diagram and a short narrative.
- [ ] Error paths and degraded modes are noted where they matter.
