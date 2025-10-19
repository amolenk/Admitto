using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

namespace Amolenk.Admitto.IntegrationTests.UseCases.Teams;

public class CreateTeamRequestBuilder
{
    private string _slug = "default-team";
    private string _name = "Default Team";
    private string _email = "team@example.com";
    private string _emailServiceConnectionString = "host=localhost;port=1025";

    public CreateTeamRequestBuilder WithSlug(string slug)
    {
        _slug = slug;
        return this;
    }

    public CreateTeamRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public CreateTeamRequestBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public CreateTeamRequestBuilder WithEmailServiceConnectionString(string emailServiceConnectionString)
    {
        _emailServiceConnectionString = emailServiceConnectionString;
        return this;
    }

    public CreateTeamRequest Build()
    {
        return new CreateTeamRequest(_slug, _name, _email!, _emailServiceConnectionString!);
    }
}
