using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Registration;

public class ShowRegistrationStatusCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        TeamEventSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await admittoService.QueryAsync(
            client => client.GetRegistrationOpenStatusAsync(teamSlug, eventSlug, cancellationToken));

        if (response is null) return 1;

        var grid = new Grid();
        grid.AddColumn(new GridColumn { Width = 16 });
        grid.AddColumn();

        grid.AddRow("Is open:", response.IsOpen ? "Yes" : "No");
        grid.AddRow("Event active:", response.IsEventActive ? "Yes" : "No");
        if (response.WindowOpensAt is not null)
        {
            grid.AddRow("Window opens:", response.WindowOpensAt.Value.ToString("O"));
        }
        if (response.WindowClosesAt is not null)
        {
            grid.AddRow("Window closes:", response.WindowClosesAt.Value.ToString("O"));
        }

        AnsiConsole.Write(grid);
        return 0;
    }
}
