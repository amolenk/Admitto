using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Team;

public class ListTeamsCommand(IAdmittoService admittoService) : AsyncCommand<TeamSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamSettings settings, CancellationToken cancellationToken)
    {
        var response = await admittoService.QueryAsync(client => client.GetTeamsAsync(cancellationToken));
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Slug");
        table.AddColumn("Name");
        table.AddColumn("Email");

        foreach (var team in response.Teams ?? [])
        {
            table.AddRow(team.Slug!, team.Name!, team.Email!);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
