using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

namespace Amolenk.Admitto.Api.Tests.Registrations.SelfChangeTickets;

internal sealed class SelfChangeTicketsFixture
{
    public const string AttendeeEmail = "alice@example.com";
    public static readonly TicketTypeId GeneralAdmissionId = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    public static readonly TicketTypeId WorkshopId = TicketTypeId.From(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    public TeamId TeamId { get; private set; } = TeamId.New();
    public TicketedEventId EventId { get; private set; } = TicketedEventId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();
    public string ApiKey => ApiKeyTestHelper.TestRawKey;

    public string ChangeTicketsRoute =>
        $"/api/teams/{TeamId.Value}/events/{EventId.Value}/registrations/{RegistrationId.Value}/tickets";

    private SelfChangeTicketsFixture() { }

    public static SelfChangeTicketsFixture WithOpenRegistration() => new();

    public async ValueTask SetupAsync(
        EndToEndTestEnvironment environment,
        bool alreadyCancelled = false,
        bool registrationWindowClosed = false,
        int workshopCapacity = 20,
        int workshopUsed = 0)
    {
        var team = new TeamBuilder()
            .Build();
        TeamId = team.Id;

        var eventId = TicketedEventId.New();
        EventId = eventId;

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            team.Id,
            EventName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

        if (!registrationWindowClosed)
        {
            ticketedEvent.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30)));
        }

        var catalog = TicketCatalog.Create(eventId, team.Id);
        catalog.AddTicketType(GeneralAdmissionId, TicketTypeName.From("General Admission"), [], 100);
        catalog.AddTicketType(WorkshopId, TicketTypeName.From("Workshop"), [], workshopCapacity);

        // Simulate used capacity for workshop
        for (var i = 0; i < workshopUsed; i++)
        {
            catalog.Claim([WorkshopId], enforce: true);
        }

        var registration = Registration.Create(
            team.Id,
            eventId,
            EmailAddress.From(AttendeeEmail),
            FirstName.From("Alice"),
            LastName.From("Test"),
            [new TicketTypeSnapshot(GeneralAdmissionId, TicketTypeName.From("General Admission"), [])]);
        RegistrationId = registration.Id;

        if (alreadyCancelled)
            registration.Cancel(CancellationReason.AttendeeRequest);

        await environment.OrganizationDatabase.SeedAsync(db =>
        {
            db.Teams.Add(team);
            db.ApiKeys.Add(ApiKeyTestHelper.CreateApiKeyEntity(team.Id));
        });
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
            db.Registrations.Add(registration);
        });
    }
}
