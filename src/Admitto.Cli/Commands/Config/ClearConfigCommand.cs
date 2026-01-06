using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Config;

public class ClearConfigSettings : CommandSettings
{
    [CommandOption("--defaultTeam")]
    [Description("Clears the default team slug.")]
    public required bool DefaultTeam { get; set; }

    [CommandOption("--defaultEvent")]
    [Description("Clears the default event slug.")]
    public required bool DefaultEvent { get; set; }
}

public class ClearConfigCommand(IConfigService configService) : Command<ClearConfigSettings>
{
    public override int Execute(CommandContext context, ClearConfigSettings settings, CancellationToken cancellationToken)
    {
        if (settings.DefaultTeam)
        {
            configService.DefaultTeam = null;
        }

        if (settings.DefaultEvent)
        {
            configService.DefaultEvent = null;
        }
        
        return 0;
    }
}