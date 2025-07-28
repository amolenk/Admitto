namespace Amolenk.Admitto.Cli.Commands.Config;

public class SetConfigSettings : ConfigSettings
{
    [CommandOption("--endpoint")]
    public required string? Endpoint { get; set; }

    [CommandOption("--defaultTeam")]
    public required string? DefaultTeam { get; set; }

    [CommandOption("--defaultEvent")]
    public required string? DefaultEvent { get; set; }
}

public class SetConfigCommand : ConfigCommandBase<SetConfigSettings>
{
    public override int Execute(CommandContext context, SetConfigSettings settings)
    {
        UpdateConfig(config =>
        {
            if (settings.Endpoint is not null)
            {
                config[ConfigSettings.EndpointSetting] = settings.Endpoint;
            }
            if (settings.DefaultTeam is not null)
            {
                config[ConfigSettings.DefaultTeamSetting] = settings.DefaultTeam;
            }
            if (settings.DefaultEvent is not null)
            {
                config[ConfigSettings.DefaultEventSetting] = settings.DefaultEvent;
            }
        });
        
        return 0;
    }
}