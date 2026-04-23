using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Team.Email;

public class ShowTeamEmailCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<TeamSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        TeamSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var response = await admittoService.QueryAsync(
            client => client.GetTeamEmailSettingsAsync(teamSlug, null, cancellationToken));

        if (response is null)
        {
            AnsiConsoleExt.WriteErrorMessage("No email settings configured for this team.");
            return 1;
        }

        AnsiConsole.Write(new Rule($"Email settings for team '{teamSlug}'")
        {
            Justification = Justify.Left,
            Style = Style.Parse("cyan")
        });

        var grid = new Grid();
        grid.AddColumn(new GridColumn { Width = 20 });
        grid.AddColumn();

        grid.AddRow("SMTP host:", response.SmtpHost);
        grid.AddRow("SMTP port:", response.SmtpPort.ToString());
        grid.AddRow("From address:", response.FromAddress);
        grid.AddRow("Auth mode:", response.AuthMode.ToString());
        grid.AddRow("Username:", response.Username ?? "-");
        grid.AddRow("Password set:", response.HasPassword ? "Yes" : "No");
        grid.AddRow("Version:", response.Version.ToString());

        AnsiConsole.Write(grid);
        return 0;
    }
}
