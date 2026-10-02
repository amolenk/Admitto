using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using TeamBuilder = Amolenk.Admitto.Testing.Builders.Organization.Application.TeamBuilder;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Api.Tests.Registrations.PromoteWaitlistEntry;

internal sealed class PromoteWaitlistEntryFixture
{
    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    public TicketTypeId WorkshopId { get; } = TicketTypeId.New();
    public Guid VipEntryId { get; private set; }

    public string RouteFor(Guid entryId) =>
        $"/admin/teams/{TeamId}/events/{EventId}/ticket-types/{WorkshopId.Value}/waitlist/{entryId}/promote";

    public string Route => RouteFor(VipEntryId);

    private PromoteWaitlistEntryFixture() { }

    /// <summary>
    /// A sold-out, waitlist-mode ticket type with two attendees queued; the VIP is second in line.
    /// </summary>
    public static PromoteWaitlistEntryFixture WithVipSecondInQueue() => new();

    public async ValueTask SetupAsync(EndToEndTestEnvironment environment)
    {
        var team = new TeamBuilder().Build();
        TeamId = team.Id.Value;

        var eventId = TicketedEventId.New();
        EventId = eventId.Value;
        var ticketTypeId = WorkshopId;

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
        catalog.AddTicketType(ticketTypeId, TicketTypeName.From("Workshop"), [], 1, waitlistEnabled: true);
        catalog.Claim([ticketTypeId], ClaimMode.Public);

        var waitlist = Waitlist.Create(eventId, ticketTypeId, team.Id);
        waitlist.AddEntry(EmailAddress.From("first@example.com"), DateTimeOffset.UtcNow, catalog, RegistrationId.New());
        waitlist.AddEntry(EmailAddress.From("vip@example.com"), DateTimeOffset.UtcNow.AddMinutes(1), catalog, RegistrationId.New());
        VipEntryId = waitlist.Entries.Single(e => e.Position == 2).Id.Value;

        await environment.OrganizationDatabase.SeedAsync(db => db.Teams.Add(team));
        await environment.RegistrationsDatabase.SeedAsync(db =>
        {
            db.TicketedEvents.Add(ticketedEvent);
            db.TicketCatalogs.Add(catalog);
            db.Waitlists.Add(waitlist);
        });
    }
}
