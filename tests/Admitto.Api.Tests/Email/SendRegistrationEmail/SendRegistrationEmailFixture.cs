using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Email.SendRegistrationEmail;

internal sealed class SendRegistrationEmailFixture
{
    public static readonly TicketTypeId TicketTypeId = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    public const string RecipientEmail = "attendee@example.com";

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public string RegisterRoute =>
        $"/admin/teams/{TeamId}/events/{EventId}/registrations";

    private SendRegistrationEmailFixture() { }

    public static SendRegistrationEmailFixture HappyFlow() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();

        var eventId = TicketedEventId.New();

        TeamId = team.Id.Value;
        EventId = eventId.Value;

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            team.Id,
            EventName.From("MailConf"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(60),
            DateTimeOffset.UtcNow.AddDays(61),
                TimeZoneId.From("UTC"));
        ticketedEvent.ConfigureRegistrationPolicy(
            TicketedEventRegistrationPolicy.Create(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30)));

        var catalog = TicketCatalog.Create(eventId, team.Id);
        catalog.AddTicketType(TicketTypeId, TicketTypeName.From("General Admission"), [], 100);

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
        });

        // Seed the Email-owned rendering projection directly. In production this
        // row is maintained by EventEmailContextProjector from integration
        // events; the fixture seeds the event synchronously, so we materialise
        // the equivalent projection state here.
        await environment.EmailDatabase.SeedAsync(db =>
        {
            var context = EventEmailContextView.CreatePartial(
                team.Id, eventId, DateTimeOffset.UtcNow);
            context.UpdateEventContext(
                ticketedEventVersion: 0,
                "MailConf",
                "https://example.com",
                ticketedEvent.PublicSlug.Value,
                "UTC",
                selfServiceTicketTypeCount: 1,
                reconfirmPolicy: null,
                isArchived: false,
                DateTimeOffset.UtcNow);
            var teamContext = TeamEmailContextView.Create(
                team.Id,
                team.Name.Value,
                team.AccentColor.Value,
                team.Version,
                DateTimeOffset.UtcNow);
            db.EventEmailContexts.Add(context);
            db.TeamEmailContexts.Add(teamContext);
        });
    }
}
