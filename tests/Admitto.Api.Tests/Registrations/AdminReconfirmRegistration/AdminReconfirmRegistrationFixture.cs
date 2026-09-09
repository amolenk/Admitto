using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.AdminReconfirmRegistration;

internal sealed class AdminReconfirmRegistrationFixture
{
    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    public string Route => $"/admin/teams/{TeamId}/events/{EventId}/registrations/{RegistrationId.Value}/reconfirm";

    private bool _cancelRegistration;
    private bool _archiveEvent;

    private AdminReconfirmRegistrationFixture() { }

    public static AdminReconfirmRegistrationFixture ActiveRegistration() => new();

    public static AdminReconfirmRegistrationFixture WithCancelledRegistration() =>
        new() { _cancelRegistration = true };

    public static AdminReconfirmRegistrationFixture WithArchivedEvent() =>
        new() { _archiveEvent = true };

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

        var catalog = TicketCatalog.Create(eventId, team.Id);
        if (_archiveEvent)
        {
            catalog.MarkEventArchived();
        }

        var registration = Registration.Create(
            team.Id,
            eventId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Test"),
            [new TicketTypeSnapshot(TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), TicketTypeName.From("General Admission"), [])]);
        RegistrationId = registration.Id;

        if (_cancelRegistration)
        {
            registration.Cancel(CancellationReason.AttendeeRequest);
        }

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
            db.Registrations.Add(registration);
        });
    }
}
