using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Events;

public class ListEventsCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : EventCommandBase<TeamSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        
        var response = await CallApiAsync(async client => await client.Teams[teamSlug].Events.GetAsync());
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Slug");
        table.AddColumn("Name");
        table.AddColumn("Status");

        foreach (var ticketedEvent in response.TicketedEvents ?? [])
        {
            var status = GetStatusString(ticketedEvent.RegistrationStartDateTime!.Value,
                ticketedEvent.RegistrationEndDateTime!.Value, ticketedEvent.StartTime!.Value, ticketedEvent.EndTime!.Value);

            table.AddRow(ticketedEvent.Slug!, ticketedEvent.Name!, status);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
