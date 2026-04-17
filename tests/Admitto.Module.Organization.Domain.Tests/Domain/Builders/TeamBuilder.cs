using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;

public class TeamBuilder
{
    public static readonly Slug DefaultSlug = Slug.From("test-team");
    public static readonly DisplayName DefaultName = DisplayName.From("Test Team");
    public static readonly EmailAddress DefaultEmailAddress = EmailAddress.From("team@example.com");

    private Slug _slug = DefaultSlug;
    private DisplayName _name = DefaultName;
    private EmailAddress _emailAddress = DefaultEmailAddress;
    private bool _archived;

    public TeamBuilder WithSlug(string slug)
    {
        _slug = Slug.From(slug);
        return this;
    }

    public TeamBuilder WithName(string name)
    {
        _name = DisplayName.From(name);
        return this;
    }

    public TeamBuilder WithEmailAddress(string emailAddress)
    {
        _emailAddress = EmailAddress.From(emailAddress);
        return this;
    }

    public TeamBuilder AsArchived()
    {
        _archived = true;
        return this;
    }

    public Team Build()
    {
        var team = Team.Create(_slug, _name, _emailAddress);

        if (_archived)
        {
            team.Archive(DateTimeOffset.UtcNow);
        }

        return team;
    }
}
