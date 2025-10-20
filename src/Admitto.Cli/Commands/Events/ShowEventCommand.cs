namespace Amolenk.Admitto.Cli.Commands.Events;

public class ShowEventCommand(
    IAccessTokenProvider accessTokenProvider, 
    IConfiguration configuration,
    OutputService outputService)
    : EventCommandBase<TeamEventSettings>(accessTokenProvider, configuration, outputService)
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var response = await CallApiAsync(async client => await client.Teams[teamSlug].Events[eventSlug].GetAsync());
        if (response?.Slug is null) return 1;

        outputService.Write(new Rule(response.Name!) { Justification = Justify.Left, Style = Style.Parse("blue") });

        var grid = new Grid();
        grid.AddColumn(new GridColumn { Width = 20 });
        grid.AddColumn();

        grid.AddRow(
            "Status:",
            GetStatusString(
                response.StartsAt!.Value,
                response.EndsAt!.Value,
                response.RegistrationOpensAt,
                response.RegistrationClosesAt));

        if (response.RegistrationOpensAt.HasValue)
        {
            grid.AddRow("Registration opens:", response.RegistrationOpensAt.Value.Format(true));
        }

        if (response.RegistrationClosesAt.HasValue)
        {
            grid.AddRow("Registration closes:", response.RegistrationClosesAt.Value.Format(true));
        }

        grid.AddRow("Event starts:", response.StartsAt!.Value.Format(true));
        grid.AddRow("Event ends:", response.EndsAt!.Value.Format(true));

        outputService.Write(grid);

        foreach (var ticketType in response.TicketTypes ?? [])
        {
            outputService.Write(
                new Rule($"{ticketType.Name} tickets") { Justification = Justify.Left, Style = Style.Parse("blue") });

            var remainingCapacity = Math.Max(0, ticketType.MaxCapacity!.Value - ticketType.UsedCapacity!.Value);

            grid = new Grid();
            grid.AddColumn(new GridColumn { Width = 20 });
            grid.AddColumn();

            grid.AddRow("Slug:", ticketType.Slug!);
            grid.AddRow("Slot(s):", string.Join(", ", ticketType.SlotNames ?? []));
            grid.AddRow(
                new Text("Capacity"),
                new BreakdownChart()
                    .AddItem("Registered", ticketType.UsedCapacity!.Value, Color.Blue)
                    .AddItem("Available", remainingCapacity, Color.Yellow));

            outputService.Write(grid);
        }

        return 0;
    }
}