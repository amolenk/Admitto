using Amolenk.Admitto.Core.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
namespace Amolenk.Admitto.Core.Module.Organization.Tests.Application.Builders;

public class TeamBuilder
{
    private DisplayName _name = DisplayName.From("Test Team");
    private EmailAddress _email = EmailAddress.From("team@example.com");
    private bool _archived;

    public TeamBuilder WithName(string name)
    {
        _name = DisplayName.From(name);
        return this;
    }

    public TeamBuilder WithEmail(string email)
    {
        _email = EmailAddress.From(email);
        return this;
    }

    public TeamBuilder AsArchived()
    {
        _archived = true;
        return this;
    }

    public Team Build()
    {
        var team = Team.Create(_name, _email);
        if (_archived)
        {
            team.Archive(DateTimeOffset.UtcNow);
        }
        return team;
    }
}