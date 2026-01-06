using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Events;

public class ShowEventCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await apiService.CallApiAsync(async client => await client.Teams[teamSlug].Events[eventSlug].GetAsync());
        if (response?.Slug is null) return 1;

        AnsiConsole.Write(new Rule(response.Name!) { Justification = Justify.Left, Style = Style.Parse("cyan") });

        var grid = new Grid();
        grid.AddColumn(new GridColumn { Width = 20 });
        grid.AddColumn();

        grid.AddRow(
            "Status:",
            EventFormatHelper.GetStatusString(
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

        AnsiConsole.Write(grid);

        foreach (var ticketType in response.TicketTypes ?? [])
        {
            AnsiConsole.Write(
                new Rule($"{ticketType.Name} tickets") { Justification = Justify.Left, Style = Style.Parse("cyan") });

            var remainingCapacity = Math.Max(0, ticketType.MaxCapacity!.Value - ticketType.UsedCapacity!.Value);

            grid = new Grid();
            grid.AddColumn(new GridColumn { Width = 20 });
            grid.AddColumn();

            grid.AddRow("Slug:", ticketType.Slug!);
            grid.AddRow("Slot(s):", string.Join(", ", ticketType.SlotNames ?? []));

            if (ticketType.MaxCapacity > 0)
            {
                grid.AddRow(
                    new Text("Capacity"),
                    new BreakdownChart()
                        {
                            ValueColor = Color.White
                        }
                        .AddItem("Registered", ticketType.UsedCapacity!.Value, Color.Green)
                        .AddItem("Available", remainingCapacity, Color.Grey));
            }
            else
            {
                grid.AddRow("Capacity:", "[red]Unavailable[/]");
            }

            AnsiConsole.Write(grid);
        }

        return 0;
    }
}