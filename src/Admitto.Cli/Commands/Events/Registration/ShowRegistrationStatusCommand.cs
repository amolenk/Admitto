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
            client => client.GetTicketedEventDetailsAsync(teamSlug, eventSlug, cancellationToken));

        if (response is null) return 1;

        var grid = new Grid();
        grid.AddColumn(new GridColumn { Width = 16 });
        grid.AddColumn();

        grid.AddRow("Is open:", response.IsRegistrationOpen ? "Yes" : "No");
        grid.AddRow("Event status:", response.Status.ToString());
        if (response.RegistrationPolicy is { } policy)
        {
            grid.AddRow("Window opens:", policy.OpensAt.ToString("O"));
            grid.AddRow("Window closes:", policy.ClosesAt.ToString("O"));
            if (!string.IsNullOrEmpty(policy.AllowedEmailDomain))
            {
                grid.AddRow("Allowed domain:", policy.AllowedEmailDomain);
            }
        }
        else
        {
            grid.AddRow("Policy:", "[yellow]not configured[/]");
        }

        AnsiConsole.Write(grid);
        return 0;
    }
}
