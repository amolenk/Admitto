using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events;

public class ListEventsCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<TeamSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var response =
            await admittoService.QueryAsync(client => client.GetTicketedEventsAsync(teamSlug, cancellationToken));
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Slug");
        table.AddColumn("Name");
        table.AddColumn("Status");

        foreach (var ticketedEvent in response.TicketedEvents ?? [])
        {
            var status = EventFormatHelper.GetStatusString(
                ticketedEvent.StartsAt,
                ticketedEvent.EndsAt,
                ticketedEvent.RegistrationOpensAt,
                ticketedEvent.RegistrationClosesAt);

            table.AddRow(ticketedEvent.Slug!, ticketedEvent.Name!, status);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
