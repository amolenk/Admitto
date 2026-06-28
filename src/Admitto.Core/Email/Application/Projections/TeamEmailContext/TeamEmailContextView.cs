using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Abstractions;

namespace Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;

public sealed class TeamEmailContextView : IIsVersioned
{
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
    public string? TeamName { get; private set; }
    public EmailAccentColor? AccentColor { get; private set; }
    public uint TeamVersion { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastUpdatedAt { get; private set; }
    public uint Version { get; set; }

    public static TeamEmailContextView CreatePartial(TeamId teamId, DateTimeOffset now) => new(teamId, now);

    public bool UpdateTeamContext(string teamName, string accentColor, uint teamVersion, DateTimeOffset now)
    {
        if (teamVersion < TeamVersion)
            return false;

        TeamName = teamName;
        AccentColor = EmailAccentColor.From(accentColor);
        TeamVersion = teamVersion;
        LastUpdatedAt = now;
        return true;
    }

    public bool HasRequiredRenderingContext => AccentColor.HasValue;
}
