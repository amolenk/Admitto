using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class ListAttendeesCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await admittoService.QueryAsync(client =>
            client.GetAttendeesAsync(teamSlug, eventSlug, cancellationToken));
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Email");
        table.AddColumn("Name");
        table.AddColumn("Status");
        table.AddColumn("Last updated");

        foreach (var attendee in response.Attendees.OrderBy(r => r.LastChangedAt))
        {
            table.AddRow(
                attendee.Email!,
                $"{attendee.FirstName} {attendee.LastName}",
                attendee.Status.Format(),
                attendee.LastChangedAt.Format());
        }

        AnsiConsole.Write(table);
        return 0;
    }
}