using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetRegistrationDetail;

internal sealed class GetRegistrationDetailFixture
{
    public const string TicketTypeSlug = "general-admission";

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    private GetRegistrationDetailFixture() { }

    public static GetRegistrationDetailFixture WithActiveRegistration() => new();

    public string RegistrationRoute =>
        $"/admin/teams/{TeamId}/events/{EventId}/registrations/{RegistrationId.Value}";

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();
        TeamId = team.Id.Value;

        var eventId = TicketedEventId.New();
        EventId = eventId.Value;

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

        var registration = Registration.Create(
            team.Id,
            eventId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Doe"),
            [new TicketTypeSnapshot(Slug.From(TicketTypeSlug), TicketTypeName.From("General Admission"), [])]);
        RegistrationId = registration.Id;

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.Registrations.Add(registration);
        });
    }
}
