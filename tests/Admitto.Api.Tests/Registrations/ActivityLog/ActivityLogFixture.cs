using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Core.Organization.Tests.Application.Builders.TeamBuilder;

namespace Amolenk.Admitto.Api.Tests.Registrations.ActivityLog;

internal sealed class ActivityLogFixture
{
    public const string TicketTypeSlug = "general-admission";

    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }

    public string RegisterRoute =>
        $"/admin/teams/{TeamId}/events/{EventId}/registrations";

    public string RegistrationDetailRoute(Guid registrationId) =>
        $"/admin/teams/{TeamId}/events/{EventId}/registrations/{registrationId}";

    public string CancelRoute(Guid registrationId) =>
        $"/admin/teams/{TeamId}/events/{EventId}/registrations/{registrationId}/cancel";

    private ActivityLogFixture() { }

    public static ActivityLogFixture HappyFlow() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .Build();

        var eventId = TicketedEventId.New();

        TeamId = team.Id.Value;
        EventId = eventId.Value;

        var ticketedEvent = TicketedEvent.Create(
            eventId,
            team.Id,
            DisplayName.From("DevConf ActivityLog"),
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
        catalog.AddTicketType(Slug.From(TicketTypeSlug), DisplayName.From("General Admission"), [], 100);

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
        });
    }
}
