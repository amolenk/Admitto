using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;

public class TeamBuilder
{
    private const string DefaultName = "Default Team";
    private static readonly TeamId DefaultId = TeamId.FromName(DefaultName);

    private string _name = DefaultName;
    private EmailSettings _emailSettings = new EmailSettingsBuilder().Build();

    public TeamBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TeamBuilder WithEmailSettings(EmailSettings emailSettings)
    {
        _emailSettings = emailSettings;
        return this;
    }

    public Team Build()
    {
        return Team.Create(_name, _emailSettings);
    }
}