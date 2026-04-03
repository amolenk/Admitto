using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.GetTicketedEvents;

internal sealed class GetTicketedEventsFixture
{
    public Guid TeamId { get; private set; }
    public string ActiveEventSlug { get; } = "active-conf";
    public string CancelledEventSlug { get; } = "cancelled-conf";
    public string ArchivedEventSlug { get; } = "archived-conf";

    private GetTicketedEventsFixture()
    {
    }

    /// <summary>Seeds an active, a cancelled, and an archived event for the same team.</summary>
    public static GetTicketedEventsFixture TeamWithActiveAndCancelledAndArchivedEvents() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var team = new TeamBuilder().WithSlug("acme").WithName("Acme Events").Build();

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
        });

        TeamId = team.Id.Value;
        var teamId = team.Id;

        var activeEvent = TicketedEvent.Create(
            teamId,
            Slug.From(ActiveEventSlug),
            DisplayName.From("Active Conf"),
            AbsoluteUrl.From("https://active.acme.org"),
            AbsoluteUrl.From("https://tickets.acme.org/"),
            new TimeWindow(
                new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 7, 2, 17, 0, 0, TimeSpan.Zero)));

        var cancelledEvent = TicketedEvent.Create(
            teamId,
            Slug.From(CancelledEventSlug),
            DisplayName.From("Cancelled Conf"),
            AbsoluteUrl.From("https://cancelled.acme.org"),
            AbsoluteUrl.From("https://tickets.acme.org/"),
            new TimeWindow(
                new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 2, 17, 0, 0, TimeSpan.Zero)));
        cancelledEvent.Cancel();

        var archivedEvent = TicketedEvent.Create(
            teamId,
            Slug.From(ArchivedEventSlug),
            DisplayName.From("Archived Conf"),
            AbsoluteUrl.From("https://archived.acme.org"),
            AbsoluteUrl.From("https://tickets.acme.org/"),
            new TimeWindow(
                new DateTimeOffset(2025, 9, 1, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 9, 2, 17, 0, 0, TimeSpan.Zero)));
        archivedEvent.Archive();

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.TicketedEvents.Add(activeEvent);
            dbContext.TicketedEvents.Add(cancelledEvent);
            dbContext.TicketedEvents.Add(archivedEvent);
        });
    }
}
