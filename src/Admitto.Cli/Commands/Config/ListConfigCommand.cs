using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Config;

public class GetConfigCommand(IConfigService configService)
    : Command
{
    public override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        var table = new Table();
        table.AddColumn("Setting");
        table.AddColumn("Value");

        table.AddRow("Default Team", configService.DefaultTeam ?? "[grey]<not set>[/]");
        table.AddRow("Default Event", configService.DefaultEvent ?? "[grey]<not set>[/]");

        AnsiConsole.Write(table);

        return 0;
    }
}