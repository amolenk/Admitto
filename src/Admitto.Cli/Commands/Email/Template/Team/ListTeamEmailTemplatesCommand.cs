using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Team;

public class ListTeamEmailTemplatesCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<TeamSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        TeamSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var response =
            await admittoService.QueryAsync(client => client.GetTeamEmailTemplatesAsync(teamSlug, cancellationToken));
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Email type");
        table.AddColumn("Status");

        foreach (var emailTemplate in response.EmailTemplates ?? [])
        {
            var status = emailTemplate.IsCustom ? "Custom" : "[grey]Default[/]";

            table.AddRow(emailTemplate.Type!, status);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}