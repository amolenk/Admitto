using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events;

public class ShowEventCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await admittoService.QueryAsync(
            client => client.GetTicketedEventAsync(teamSlug, eventSlug, cancellationToken));

        if (response is null) return 1;
        
        AnsiConsole.Write(new Rule(response.Name!) { Justification = Justify.Left, Style = Style.Parse("cyan") });

        var grid = new Grid();
        grid.AddColumn(new GridColumn { Width = 20 });
        grid.AddColumn();

        grid.AddRow(
            "Status:",
            EventFormatHelper.GetStatusString(
                response.StartsAt,
                response.EndsAt,
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
        
        grid.AddRow("Event starts:", response.StartsAt.Format(true));
        grid.AddRow("Event ends:", response.EndsAt.Format(true));

        AnsiConsole.Write(grid);

        if (response.ReconfirmPolicy is not null)
        {
            var policy = response.ReconfirmPolicy;
            
            AnsiConsole.Write(new Rule("Reconfirm Policy") { Justification = Justify.Left, Style = Style.Parse("cyan") });

            grid = new Grid();
            grid.AddColumn(new GridColumn { Width = 20 });
            grid.AddColumn();

            grid.AddRow("Window opens at:", policy.WindowOpensAt.Format(true));
            grid.AddRow("Window closes at:", policy.WindowClosesAt.Format(true));
            grid.AddRow("Initial delay after registration:", policy.InitialDelayAfterRegistration.Humanize());
            grid.AddRow("Reminder interval:", policy.ReminderInterval > TimeSpan.Zero
                ? policy.ReminderInterval.Humanize()
                : "(Not set)");
            
            AnsiConsole.Write(grid);
        }
        
        foreach (var ticketType in response.TicketTypes ?? [])
        {
            AnsiConsole.Write(
                new Rule($"{ticketType.Name} tickets") { Justification = Justify.Left, Style = Style.Parse("cyan") });

            var remainingCapacity = Math.Max(0, ticketType.MaxCapacity - ticketType.UsedCapacity);

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
                        .AddItem("Registered", ticketType.UsedCapacity, Color.Green)
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