using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Events;

public class ShowEventCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : EventCommandBase<TeamEventSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var response = await CallApiAsync(async client => await client.Teams[teamSlug].Events[eventSlug].GetAsync());
        if (response?.Slug is null) return 1;

        AnsiConsole.Write(new Rule(response.Name!) { Justification = Justify.Left, Style = Style.Parse("blue") });

        var grid = new Grid();
        grid.AddColumn(new GridColumn { Width = 20 });
        grid.AddColumn();

        grid.AddRow(
            "Status:",
            GetStatusString(
                response.RegistrationStartDateTime!.Value,
                response.RegistrationEndDateTime!.Value,
                response.StartDateTime!.Value,
                response.EndDateTime!.Value));
        grid.AddRow("Registration opens:", response.RegistrationStartDateTime!.Value.Format(true));
        grid.AddRow("Registration closes:", response.RegistrationEndDateTime!.Value.Format(true));
        grid.AddRow("Event starts:", response.StartDateTime!.Value.Format(true));
        grid.AddRow("Event ends:", response.EndDateTime!.Value.Format(true));

        AnsiConsole.Write(grid);

        foreach (var ticketType in response.TicketTypes ?? [])
        {
            AnsiConsole.Write(
                new Rule($"{ticketType.Name} tickets") { Justification = Justify.Left, Style = Style.Parse("blue") });

            var remainingCapacity = Math.Max(0, ticketType.MaxCapacity!.Value - ticketType.UsedCapacity!.Value);

            grid = new Grid();
            grid.AddColumn(new GridColumn { Width = 20 });
            grid.AddColumn();

            grid.AddRow("Slug:", ticketType.Slug!);
            grid.AddRow("Slot(s):", ticketType.SlotName!);
            grid.AddRow(
                new Text("Capacity"),
                new BreakdownChart()
                    .AddItem("Registered", ticketType.UsedCapacity!.Value, Color.Blue)
                    .AddItem("Available", remainingCapacity, Color.Yellow));

            AnsiConsole.Write(grid);
        }

        return 0;
    }
}