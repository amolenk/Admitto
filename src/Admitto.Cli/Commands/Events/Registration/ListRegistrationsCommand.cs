using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Registration;

public class ListRegistrationsCommand(IAdmittoService admittoService, IConfigService configService)
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
            client => client.GetRegistrationsAsync(teamSlug, eventSlug, cancellationToken));

        if (response is null) return 1;

        if (response.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No registrations yet.[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn("Email");
        table.AddColumn("Tickets");
        table.AddColumn("Registered");

        foreach (var item in response)
        {
            var ticketSlugs = string.Join(", ", item.Tickets.Select(t => t.Slug));
            table.AddRow(item.Email, ticketSlugs, item.CreatedAt.ToString("O"));
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
