using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamManagement.ArchiveTeam;

internal sealed class ArchiveTeamFixture
{
    public Guid TeamId { get; private set; }
    public uint TeamVersion { get; private set; }

    private readonly bool _archived;
    private readonly bool _hasActiveEvent;

    private ArchiveTeamFixture(bool archived = false, bool hasActiveEvent = false)
    {
        _archived = archived;
        _hasActiveEvent = hasActiveEvent;
    }

    public static ArchiveTeamFixture ActiveTeamWithNoEvents() => new();

    public static ArchiveTeamFixture AlreadyArchivedTeam() => new(archived: true);

    public static ArchiveTeamFixture ActiveTeamWithUpcomingEvent() => new(hasActiveEvent: true);

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var builder = new TeamBuilder().WithSlug("acme").WithName("Acme Events");

        if (_archived)
        {
            builder = builder.AsArchived();
        }

        var team = builder.Build();

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
        });

        // Capture team identity and EF-assigned version
        TeamId = team.Id.Value;
        TeamVersion = team.Version;

        if (_hasActiveEvent)
        {
            var activeEvent = TicketedEvent.Create(
                team.Id,
                Slug.From("upcoming-event"),
                DisplayName.From("Upcoming Event"),
                AbsoluteUrl.From("https://example.com/events/upcoming"),
                AbsoluteUrl.From("https://tickets.example.com"),
                new TimeWindow(
                    DateTimeOffset.UtcNow.AddDays(1),
                    DateTimeOffset.UtcNow.AddDays(2)));

            await environment.Database.SeedAsync(dbContext =>
            {
                dbContext.TicketedEvents.Add(activeEvent);
            });
        }
    }
}
