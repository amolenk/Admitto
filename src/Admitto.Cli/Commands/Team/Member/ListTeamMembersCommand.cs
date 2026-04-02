using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Team.Member;

public class ListTeamMembersCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<TeamSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        TeamSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var response = await admittoService.QueryAsync(
            client => client.ListTeamMembersAsync(teamSlug, cancellationToken));
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Email");
        table.AddColumn("Role");

        foreach (var member in response)
        {
            table.AddRow(member.Email, member.Role.ToString());
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
