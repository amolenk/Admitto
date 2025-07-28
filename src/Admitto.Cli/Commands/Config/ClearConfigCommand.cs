namespace Amolenk.Admitto.Cli.Commands.Config;

public class ClearConfigSettings : ConfigSettings
{
    [CommandOption("--endpoint")]
    public required bool Endpoint { get; set; }

    [CommandOption("--defaultTeam")]
    public required bool DefaultTeam { get; set; }

    [CommandOption("--defaultEvent")]
    public required bool DefaultEvent { get; set; }
}

public class ClearConfigCommand : ConfigCommandBase<ClearConfigSettings>
{
    public override int Execute(CommandContext context, ClearConfigSettings settings)
    {
        UpdateConfig(config =>
        {
            if (settings.Endpoint)
            {
                config.Remove(ConfigSettings.EndpointSetting);
            }

            if (settings.DefaultTeam)
            {
                config.Remove(ConfigSettings.DefaultTeamSetting);
            }

            if (settings.DefaultEvent)
            {
                config.Remove(ConfigSettings.DefaultEventSetting);
            }
        });
        
        return 0;
    }
}