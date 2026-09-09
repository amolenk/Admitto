using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.ReconfirmRegistration;

internal sealed class ReconfirmRegistrationFixture
{
    public const string AttendeeEmail = "alice@example.com";
    public static readonly TicketTypeId TicketTypeId =
        TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    public TeamId TeamId { get; private set; } = TeamId.New();
    public TicketedEventId EventId { get; private set; } = TicketedEventId.New();
    public string EventSlug { get; private set; } = string.Empty;
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();
    public string ApiKey => ApiKeyTestHelper.TestRawKey;

    private readonly bool _seedRegistration;
    private readonly bool _cancelRegistration;
    private readonly bool _closedReconfirmPolicy;

    private ReconfirmRegistrationFixture(
        bool seedRegistration,
        bool cancelRegistration,
        bool closedReconfirmPolicy = false)
    {
        _seedRegistration = seedRegistration;
        _cancelRegistration = cancelRegistration;
        _closedReconfirmPolicy = closedReconfirmPolicy;
    }

    public static ReconfirmRegistrationFixture HappyFlow() => new(
        seedRegistration: true, cancelRegistration: false);

    public static ReconfirmRegistrationFixture WithCancelledRegistration() => new(
        seedRegistration: true, cancelRegistration: true);

    public static ReconfirmRegistrationFixture WithoutRegistration() => new(
        seedRegistration: false, cancelRegistration: false);

    public static ReconfirmRegistrationFixture BelowMaximumAfterPolicyClose() => new(
        seedRegistration: true,
        cancelRegistration: false,
        closedReconfirmPolicy: true);

    public string ReconfirmRoute(Guid registrationId) =>
        $"/api/events/{EventSlug}/registrations/{registrationId}/reconfirm";

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
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

        var catalog = TicketCatalog.Create(eventId, team.Id);
        catalog.AddTicketType(
            TicketTypeId,
            TicketTypeName.From("General Admission"),
            [],
            100,
            maxReconfirmationEmails: ReconfirmationEmailLimit.From(2));
        if (_closedReconfirmPolicy)
        {
            ticketedEvent.ConfigureReconfirmPolicy(
                TicketedEventReconfirmPolicy.Create(
                    DateTimeOffset.UtcNow.AddDays(-2),
                    DateTimeOffset.UtcNow.AddMinutes(-1),
                    TimeSpan.FromHours(1)));
        }

        Registration? registration = null;
        if (_seedRegistration)
        {
            registration = Registration.Create(
                team.Id,
                eventId,
                EmailAddress.From(AttendeeEmail),
                FirstName.From("Alice"),
                LastName.From("Test"),
                [new TicketTypeSnapshot(TicketTypeId, TicketTypeName.From("General Admission"), [])]);
            RegistrationId = registration.Id;

            if (_cancelRegistration)
                registration.Cancel(CancellationReason.AttendeeRequest);
        }

        await environment.OrganizationDatabase.SeedAsync(db =>
        {
            db.Teams.Add(team);
            db.ApiKeys.Add(ApiKeyTestHelper.CreateApiKeyEntity(team.Id));
        });
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
            if (registration is not null)
                db.Registrations.Add(registration);
        });
    }
}
