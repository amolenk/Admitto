using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;

public class TeamBuilder
{
    private static readonly TeamId DefaultId = TeamId.FromName(DefaultName);
    
    private const string DefaultSlug = "default-team";
    private const string DefaultName = "Default Team";
    private const string DefaultEmail = "team@example.com";
    private const string DefaultEmailServiceConnectionString = "host=localhost;port=1025";
    
    private string _slug = DefaultSlug;
    private string _name = DefaultName;
    private string _email = DefaultEmail;
    private string _emailServiceConnectionString = DefaultEmailServiceConnectionString;

    public TeamBuilder WithSlug(string slug)
    {
        _slug = slug;
        return this;
    }

    public TeamBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TeamBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public TeamBuilder WithEmailServiceConnectionString(string emailServiceConnectionString)
    {
        _emailServiceConnectionString = emailServiceConnectionString;
        return this;
    }

    public Team Build()
    {
        return Team.Create(_slug, _name, _email, _emailServiceConnectionString);
    }
}