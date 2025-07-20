using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Events;

public class ListTicketTypesSettings : EventSettings
{
    [CommandOption("-s|--slug")]
    public string? EventSlug { get; set; }
}

public class ListTicketTypesCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : EventCommandBase<ListTicketTypesSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListTicketTypesSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        var response = await CallApiAsync(
            async client => await client.Teams[teamSlug].Events[eventSlug].TicketTypes.GetAsync());
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Slot(s)");
        table.AddColumn("Max. Capacity");

        foreach (var ticketType in response.TicketTypes ?? [])
        {
            table.AddRow(ticketType.Name!, ticketType.SlotName!, ticketType.MaxCapacity!.Value.ToString());
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
