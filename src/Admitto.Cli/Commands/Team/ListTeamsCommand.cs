using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Team;

public class ListTeamsCommand(IApiService apiService) : AsyncCommand<TeamSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamSettings settings)
    {
        var response = await apiService.CallApiAsync(async client => await client.Teams.GetAsync());
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
