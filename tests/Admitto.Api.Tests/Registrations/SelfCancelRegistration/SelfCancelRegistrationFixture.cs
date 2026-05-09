using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Core.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.SelfCancelRegistration;

internal sealed class SelfCancelRegistrationFixture
{
    public const string AttendeeEmail = "alice@example.com";

    public TeamId TeamId { get; private set; } = TeamId.New();
    public TicketedEventId EventId { get; private set; } = TicketedEventId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();
    public string ApiKey => ApiKeyTestHelper.TestRawKey;

    public string CancelRoute => $"/api/teams/{TeamId.Value}/events/{EventId.Value}/registrations/{RegistrationId.Value}/cancel";

    private SelfCancelRegistrationFixture() { }

    public static SelfCancelRegistrationFixture WithActiveRegistration() => new();
    public static SelfCancelRegistrationFixture WithCancelledRegistration() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment, bool alreadyCancelled = false)
    {
        var team = new TeamBuilder()
            .Build();
        TeamId = team.Id;

        var eventId = TicketedEventId.New();
        EventId = eventId;

        var ticketedEvent = TicketedEvent.Create(
            eventId,
            team.Id,
            DisplayName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

        var registration = Registration.Create(
            team.Id,
            eventId,
            EmailAddress.From(AttendeeEmail),
            FirstName.From("Alice"),
            LastName.From("Test"),
            [new TicketTypeSnapshot("general-admission", "General Admission", [])]);
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
            db.Registrations.Add(registration);
        });
    }
}
