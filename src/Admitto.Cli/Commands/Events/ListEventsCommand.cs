namespace Amolenk.Admitto.Cli.Commands.Events;

public class ListEventsCommand(
    IAccessTokenProvider accessTokenProvider, 
    IConfiguration configuration,
    OutputService outputService)
    : EventCommandBase<TeamSettings>(accessTokenProvider, configuration, outputService)
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
            var status = GetStatusString(
                ticketedEvent.StartsAt!.Value,
                ticketedEvent.EndsAt!.Value,
                ticketedEvent.RegistrationOpensAt,
                ticketedEvent.RegistrationClosesAt);

            table.AddRow(ticketedEvent.Slug!, ticketedEvent.Name!, status);
        }

        outputService.Write(table);
        return 0;
    }
}
