using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Testing.Builders.Organization.Domain;

public class TeamBuilder
{
    public static readonly TeamName DefaultName = TeamName.From("Test Team");

    private TeamName _name = DefaultName;
    private TeamAccentColor? _accentColor;
    private EmailAddress? _replyToEmailAddress;
    private bool _archived;

    public TeamBuilder WithName(string name)
    {
        _name = TeamName.From(name);
        return this;
    }

    public TeamBuilder AsArchived()
    {
        _archived = true;
        return this;
    }

    public TeamBuilder WithAccentColor(string accentColor)
    {
        _accentColor = TeamAccentColor.From(accentColor);
        return this;
    }

    public TeamBuilder WithReplyToEmailAddress(string replyToEmailAddress)
    {
        _replyToEmailAddress = EmailAddress.From(replyToEmailAddress);
        return this;
    }

    public Team Build()
    {
        var team = Team.Create(_name, _accentColor, _replyToEmailAddress);

        if (_archived)
        {
            team.Archive(DateTimeOffset.UtcNow);
        }

        return team;
    }
}
