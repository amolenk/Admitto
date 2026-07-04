using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.SelfUpdateRegistration;

internal sealed class SelfUpdateRegistrationFixture
{
    public const string AttendeeEmail = "alice@example.com";
    public static readonly TicketTypeId GeneralAdmissionId = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    public static readonly TicketTypeId WorkshopId = TicketTypeId.From(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private bool _seedOtherTeamApiKey;

    public TeamId TeamId { get; private set; } = TeamId.New();
    public TicketedEventId EventId { get; private set; } = TicketedEventId.New();
    public string EventSlug { get; private set; } = string.Empty;
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();
    public string ApiKey => ApiKeyTestHelper.TestRawKey;
    public string OtherTeamApiKey => ApiKeyTestHelper.TestRawKey2;

    public string UpdateRoute => $"/api/events/{EventSlug}/registrations/{RegistrationId.Value}";
    public string OldTicketsRoute => $"/api/events/{EventSlug}/registrations/{RegistrationId.Value}/tickets";

    private SelfUpdateRegistrationFixture() { }

    public static SelfUpdateRegistrationFixture WithOpenRegistration() => new();

    public static SelfUpdateRegistrationFixture WithOtherTeamApiKey() => new() { _seedOtherTeamApiKey = true };

    public async ValueTask SetupAsync(
        EndToEndTestEnvironment environment,
        bool alreadyCancelled = false,
        int workshopCapacity = 20,
        int workshopUsed = 0)
    {
        var team = new TeamBuilder().Build();
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
        EventSlug = ticketedEvent.PublicSlug.Value;
        ticketedEvent.ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy.Create(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30)));
        ticketedEvent.UpdateAdditionalDetailSchema(
        [
            AdditionalDetailField.Create("dietary", "Dietary", 200)
        ]);

        var catalog = TicketCatalog.Create(eventId, team.Id);
        catalog.AddTicketType(GeneralAdmissionId, TicketTypeName.From("General Admission"), [], 100);
        catalog.AddTicketType(WorkshopId, TicketTypeName.From("Workshop"), [], workshopCapacity);
        for (var i = 0; i < workshopUsed; i++)
            catalog.Claim([WorkshopId], enforce: true);

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

            if (_seedOtherTeamApiKey)
            {
                var otherTeam = new TeamBuilder().Build();
                db.Teams.Add(otherTeam);
                db.ApiKeys.Add(ApiKeyTestHelper.CreateApiKeyEntity2(otherTeam.Id));
            }
        });
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
            db.Registrations.Add(registration);
        });
    }
}
