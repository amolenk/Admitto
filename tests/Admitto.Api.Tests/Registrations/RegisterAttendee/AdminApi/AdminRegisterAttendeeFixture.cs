using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

namespace Amolenk.Admitto.Api.Tests.Registrations.RegisterAttendee.AdminApi;

internal sealed class AdminRegisterAttendeeFixture
{
    public const string TicketTypeSlug = "general-admission";

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public string Route => $"/admin/teams/{TeamId}/events/{EventId}/registrations";

    private AdminRegisterAttendeeFixture() { }

    public static AdminRegisterAttendeeFixture HappyFlow() => new();

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
        ticketedEvent.ConfigureRegistrationPolicy(
            TicketedEventRegistrationPolicy.Create(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30)));

        var catalog = TicketCatalog.Create(eventId);
        catalog.AddTicketType(Slug.From(TicketTypeSlug), TicketTypeName.From("General Admission"), [], 100);

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
        });
    }
}
