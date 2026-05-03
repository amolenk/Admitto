using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Module.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.ChangeAttendeeTickets;

internal sealed class ChangeAttendeeTicketsFixture
{
    public const string TeamSlug = "acme";
    public const string EventSlug = "devconf";

    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    public string Route =>
        $"/admin/teams/{TeamSlug}/events/{EventSlug}/registrations/{RegistrationId.Value}/tickets";

    private ChangeAttendeeTicketsFixture() { }

    public static ChangeAttendeeTicketsFixture WithActiveRegistration() => new();

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

        var catalog = TicketCatalog.Create(eventId);
        catalog.AddTicketType(Slug.From("general-admission"), DisplayName.From("General Admission"), [], 100);
        catalog.AddTicketType(Slug.From("workshop"), DisplayName.From("Workshop"), [], 20);

        var registration = Registration.Create(
            team.Id,
            eventId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Test"),
            [new TicketTypeSnapshot("general-admission", "General Admission", [])]);
        RegistrationId = registration.Id;

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
            db.Registrations.Add(registration);
        });
    }
}
