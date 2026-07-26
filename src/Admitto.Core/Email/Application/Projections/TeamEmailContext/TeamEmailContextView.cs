using Amolenk.Admitto.Core.Shared.Kernel.Abstractions;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;

/// <summary>
/// Email-owned, eventually-consistent read model holding the team-level facts the
/// Email module needs for branding, sender labels, and reply routing. One row per
/// <c>TeamId</c>, maintained by <see cref="TeamEmailContextProjector"/> from
/// Organization integration events.
/// <para>
/// Unlike <see cref="EventEmailContext.EventEmailContextView"/>, rows are never
/// partial: every source event (<c>TeamCreated</c>, <c>TeamDetailsUpdated</c>)
/// carries the complete field set, so a row is always created fully populated.
/// Consumers therefore do not need to null-check. A team whose event has not yet
/// reached Email simply has no row at all, which the send pipeline handles by
/// falling back to default branding.
/// </para>
/// </summary>
public sealed class TeamEmailContextView : IIsVersioned
{
    // Required for EF Core
    private TeamEmailContextView()
    {
    }

    private TeamEmailContextView(TeamId teamId, DateTimeOffset now)
    {
        TeamId = teamId;
        CreatedAt = now;
        LastUpdatedAt = now;
    }

    public TeamId TeamId { get; private set; }
    public string TeamName { get; private set; } = null!;
    public AccentColor AccentColor { get; private set; }
    public uint TeamVersion { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastUpdatedAt { get; private set; }
    public uint Version { get; set; }

    public static TeamEmailContextView Create(
        TeamId teamId,
        string teamName,
        string accentColor,
        uint teamVersion,
        DateTimeOffset now)
    {
        var view = new TeamEmailContextView(teamId, now);
        view.UpdateTeamContext(teamName, accentColor, teamVersion, now);
        return view;
    }

    public bool UpdateTeamContext(
        string teamName,
        string accentColor,
        uint teamVersion,
        DateTimeOffset now)
    {
        if (teamVersion < TeamVersion)
            return false;

        TeamName = teamName;
        AccentColor = AccentColor.From(accentColor);
        TeamVersion = teamVersion;
        LastUpdatedAt = now;
        return true;
    }
}
