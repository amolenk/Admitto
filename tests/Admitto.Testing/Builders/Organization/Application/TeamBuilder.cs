using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
namespace Amolenk.Admitto.Testing.Builders.Organization.Application;

public class TeamBuilder
{
    private TeamName _name = TeamName.From("Test Team");
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

    public Team Build()
    {
        var team = Team.Create(_name);
        if (_archived)
        {
            team.Archive(DateTimeOffset.UtcNow);
        }
        return team;
    }
}
