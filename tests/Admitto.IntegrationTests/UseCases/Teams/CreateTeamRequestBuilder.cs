using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

namespace Amolenk.Admitto.IntegrationTests.UseCases.Teams;

public class CreateTeamRequestBuilder
{
    private string _name = "Default Team";
    private EmailSettingsDto _emailSettings = EmailSettingsDto.FromEmailSettings(
        AssemblyTestFixture.EmailTestFixture.DefaultEmailSettings);
    private List<TeamMemberDto> _members = [];

    public CreateTeamRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public CreateTeamRequestBuilder WithEmailSettings(EmailSettingsDto emailSettings)
    {
        _emailSettings = emailSettings;
        return this;
    }

    public CreateTeamRequestBuilder WithMembers(IEnumerable<TeamMemberDto> members)
    {
        _members = members.ToList();
        return this;
    }

    public CreateTeamRequest Build()
    {
        return new CreateTeamRequest(_name, _emailSettings, _members);
    }
}
