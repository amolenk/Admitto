using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Config;

public class SetConfigSettings : CommandSettings
{
    [CommandOption("--defaultTeam")]
    [Description("The default team slug to set.")]
    public required string? DefaultTeam { get; set; }

    [CommandOption("--defaultEvent")]
    [Description("The default event slug to set.")]
    public required string? DefaultEvent { get; set; }
}

public class SetConfigCommand(IConfigService configService) : Command<SetConfigSettings>
{
    public override int Execute(CommandContext context, SetConfigSettings settings, CancellationToken cancellationToken)
    {
        if (settings.DefaultTeam is not null)
        {
            configService.DefaultTeam = settings.DefaultTeam;
        }

        if (settings.DefaultEvent is not null)
        {
            configService.DefaultEvent = settings.DefaultEvent;
        }
        
        return 0;
    }
}