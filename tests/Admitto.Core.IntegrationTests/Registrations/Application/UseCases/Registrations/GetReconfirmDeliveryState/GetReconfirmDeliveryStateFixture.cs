using Amolenk.Admitto.Core.Registrations.Application.UseCases;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetReconfirmDeliveryState;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.GetReconfirmDeliveryState;

internal sealed class GetReconfirmDeliveryStateFixture
{
    public TeamId TeamId { get; private init; }
    public TicketedEventId EventId { get; private init; }
    public RegistrationId RegistrationId { get; private set; }
    public RegistrationCycleId RegistrationCycleId { get; private set; }
    public TicketTypeId TicketTypeId { get; private init; }
    public DateTimeOffset Now { get; private init; }

    private GetReconfirmDeliveryStateFixture() { }

    public static GetReconfirmDeliveryStateFixture Active(DateTimeOffset now) => new()
    {
        TeamId = TeamId.New(),
        EventId = TicketedEventId.New(),
        TicketTypeId = TicketTypeId.New(),
        Now = now,
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            EventId,
            TeamId,
            EventName.From("Test Event"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            Now.AddDays(30),
            Now.AddDays(31),
            TimeZoneId.From("UTC"));
        ticketedEvent.ConfigureReconfirmPolicy(TicketedEventReconfirmPolicy.Create(
            Now.AddHours(-1), Now.AddHours(2), TimeSpan.FromHours(24)));

        var catalog = TicketCatalog.Create(EventId, TeamId);
        catalog.AddTicketType(
            TicketTypeId,
            TicketTypeName.From("General Admission"),
            [],
            100,
            maxReconfirmationEmails: ReconfirmationEmailLimit.From(2));

        var registration = Registration.Create(
            TeamId,
            EventId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Doe"),
            [new TicketTypeSnapshot(TicketTypeId, TicketTypeName.From("General Admission"), [])],
            registeredAt: Now.AddDays(-2));
        RegistrationId = registration.Id;
        RegistrationCycleId = registration.RegistrationCycleId;

        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
            db.Registrations.Add(registration);
        });
    }

    public async ValueTask<ReconfirmDeliveryState> QueryAsync(
        IntegrationTestEnvironment environment,
        DateTimeOffset? now = null)
    {
        var facade = new RegistrationsFacade(
            new GetRegistrationsHandler(environment.RegistrationsDatabase.Context),
            environment.RegistrationsDatabase.Context,
            new GetReconfirmDeliveryStateHandler(environment.RegistrationsDatabase.Context));
        return await facade.GetReconfirmDeliveryStateAsync(
            TeamId.Value,
            EventId.Value,
            new ReconfirmDeliveryQuery(
                RegistrationId.Value,
                RegistrationCycleId.Value,
                [TicketTypeId.Value],
                now ?? Now),
            CancellationToken.None);
    }

    public async ValueTask ReconfirmAsync(IntegrationTestEnvironment environment) =>
        await environment.RegistrationsDatabase.SeedAsync(db =>
            db.Registrations.Find(RegistrationId)!.Reconfirm(Now));

    public async ValueTask CancelAsync(IntegrationTestEnvironment environment) =>
        await environment.RegistrationsDatabase.SeedAsync(db =>
            db.Registrations.Find(RegistrationId)!.Cancel(CancellationReason.AttendeeRequest));

    public async ValueTask ArchiveAsync(IntegrationTestEnvironment environment) =>
        await environment.RegistrationsDatabase.SeedAsync(db =>
            db.TicketedEvents.Find(EventId)!.Archive());

    public async ValueTask DisablePolicyAsync(IntegrationTestEnvironment environment) =>
        await environment.RegistrationsDatabase.SeedAsync(db =>
            db.TicketedEvents.Find(EventId)!.ConfigureReconfirmPolicy(null));

    public async ValueTask ConfigureQuietHoursAsync(IntegrationTestEnvironment environment) =>
        await environment.RegistrationsDatabase.SeedAsync(db =>
            db.TicketedEvents.Find(EventId)!.ConfigureReconfirmPolicy(
                TicketedEventReconfirmPolicy.Create(
                    Now.AddHours(-1), Now.AddHours(2), TimeSpan.FromHours(24),
                    new TimeOnly(22, 0), new TimeOnly(8, 0))));
}
