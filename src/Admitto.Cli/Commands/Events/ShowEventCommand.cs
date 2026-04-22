using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events;

public class ShowEventCommand(IAdmittoService admittoService, IConfigService configService)
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

        AnsiConsole.Write(new Rule(response.Name) { Justification = Justify.Left, Style = Style.Parse("cyan") });

        var grid = new Grid();
        grid.AddColumn(new GridColumn { Width = 24 });
        grid.AddColumn();

        grid.AddRow("Slug:", response.Slug);
        grid.AddRow("Status:", response.Status.ToString());
        grid.AddRow("Event starts:", response.StartsAt.Format(true));
        grid.AddRow("Event ends:", response.EndsAt.Format(true));
        grid.AddRow("Website URL:", response.WebsiteUrl);
        grid.AddRow("Base URL:", response.BaseUrl);
        grid.AddRow("Version:", response.Version.ToString());
        grid.AddRow("Registration:", response.IsRegistrationOpen ? "Open" : "Closed");

        AnsiConsole.Write(grid);

        return 0;
    }
}
