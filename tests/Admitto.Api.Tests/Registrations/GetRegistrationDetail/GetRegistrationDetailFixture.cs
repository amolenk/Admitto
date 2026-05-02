using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetRegistrationDetail;

internal sealed class GetRegistrationDetailFixture
{
    public const string TeamSlug = "acme";
    public const string EventSlug = "devconf";
    public const string TicketTypeSlug = "general-admission";

    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    public static string Route(
        string teamSlug = TeamSlug,
        string eventSlug = EventSlug,
        string? registrationId = null) =>
        $"/admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId ?? Guid.NewGuid().ToString()}";

    private GetRegistrationDetailFixture() { }

    public static GetRegistrationDetailFixture WithActiveRegistration() => new();

    public string RegistrationRoute =>
        $"/admin/teams/{TeamSlug}/events/{EventSlug}/registrations/{RegistrationId.Value}";

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .WithSlug(TeamSlug)
            .Build();

        var eventId = TicketedEventId.New();

        var ticketedEvent = TicketedEvent.Create(
            eventId,
            team.Id,
            Slug.From(TeamSlug),
            Slug.From(EventSlug),
            DisplayName.From("DevConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
            TimeZoneId.From("UTC"));

        var registration = Registration.Create(
            team.Id,
            eventId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Doe"),
            [new TicketTypeSnapshot(TicketTypeSlug, "General Admission", [])]);
        RegistrationId = registration.Id;

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.Registrations.Add(registration);
        });
    }
}
