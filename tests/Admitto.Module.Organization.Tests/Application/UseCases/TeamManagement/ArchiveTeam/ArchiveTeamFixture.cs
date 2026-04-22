using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamManagement.ArchiveTeam;

internal sealed class ArchiveTeamFixture
{
    public Guid TeamId { get; private set; }
    public uint TeamVersion { get; private set; }

    private readonly bool _archived;
    private readonly bool _hasActiveEvent;
    private readonly bool _hasPendingRequest;

    private ArchiveTeamFixture(bool archived = false, bool hasActiveEvent = false, bool hasPendingRequest = false)
    {
        _archived = archived;
        _hasActiveEvent = hasActiveEvent;
        _hasPendingRequest = hasPendingRequest;
    }

    public static ArchiveTeamFixture ActiveTeamWithNoEvents() => new();

    public static ArchiveTeamFixture AlreadyArchivedTeam() => new(archived: true);

    public static ArchiveTeamFixture ActiveTeamWithUpcomingEvent() => new(hasActiveEvent: true);

    public static ArchiveTeamFixture ActiveTeamWithPendingCreationRequest() => new(hasPendingRequest: true);

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var builder = new TeamBuilder().WithSlug("acme").WithName("Acme Events");

        if (_archived)
        {
            builder = builder.AsArchived();
        }

        var team = builder.Build();

        if (_hasActiveEvent)
        {
            var request = team.RequestEventCreation(
                Slug.From("upcoming-event"),
                UserId.New(),
                DateTimeOffset.UtcNow);
            team.RegisterEventCreated(
                request.Id,
                TicketedEventId.New(),
                DateTimeOffset.UtcNow);
        }

        if (_hasPendingRequest)
        {
            team.RequestEventCreation(
                Slug.From("pending-event"),
                UserId.New(),
                DateTimeOffset.UtcNow);
        }

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
        });

        TeamId = team.Id.Value;
        TeamVersion = team.Version;
    }
}
