using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class ShowSettings : TeamEventSettings
{
    [CommandOption("--email")]
    [Description("The email address of the attendee")]
    public string? Email { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            return ValidationErrors.EmailMissing;
        }

        return base.Validate();
    }
}

public class ShowAttendeeCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<ShowSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ShowSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);
        
        var attendeeId = await apiService.FindAttendeeAsync(teamSlug, eventSlug, settings.Email!);
        if (attendeeId is null)
        {
            AnsiConsoleExt.WriteErrorMessage($"Attendee with email '{settings.Email}' not found.");
            return 1;
        }
        
        var attendeeResponse = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees[attendeeId.Value].GetAsync());
        if (attendeeResponse is null) return 1;

        var eventResponse = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].GetAsync());
        if (eventResponse is null) return 1;

        AnsiConsole.Write(new Rule(attendeeResponse.Email!) { Justification = Justify.Left, Style = Style.Parse("cyan") });

        var headerColumnWidth = Math.Max(20, eventResponse.AdditionalDetailSchemas?.Max(
            ds => ds.Name.Humanize().Length) ?? 0);
        
        var grid = new Grid();
        grid.AddColumn(new GridColumn { Width = headerColumnWidth });
        grid.AddColumn();

        grid.AddRow("Status:", attendeeResponse.RegistrationStatus!.Value.Format());
        grid.AddRow("Name:", $"{attendeeResponse.FirstName} {attendeeResponse.LastName}");

        foreach (var detailSchema in eventResponse.AdditionalDetailSchemas ?? [])
        {
            var detail = attendeeResponse.AdditionalDetails?.FirstOrDefault(
                d => d.Name == detailSchema.Name);
            
            grid.AddRow($"{detailSchema.Name.Humanize()}:", detail?.Value ?? "-");
        }

        grid.AddRow("Last updated:", attendeeResponse.LastChangedAt!.Value.Format());
        
        
        var ticketLines = attendeeResponse.Tickets?.Select(t =>
            {
                var ticketType = eventResponse.TicketTypes?.FirstOrDefault(tt => tt.Slug == t.TicketTypeSlug);
                return ticketType != null ? ticketType.Name! : t.TicketTypeSlug!;
            })
            .ToArray() ?? [];
        
        grid.AddRow("Tickets:", ticketLines.Length > 0 ? string.Join("\n", ticketLines) : "-");
        
        AnsiConsole.Write(grid);
        
        AnsiConsole.Write(new Rule("Activities") { Justification = Justify.Left, Style = Style.Parse("cyan") });

        var table = new Table();
        table.AddColumn("Date/time");
        table.AddColumn("Description");

        foreach (var activity in (attendeeResponse.Activities ?? []).OrderBy(a => a.OccuredOn))
        {
            var text = activity.EmailType is not null
                ? $"{activity.Activity.Humanize()} ({activity.EmailType})"
                : activity.Activity.Humanize();
            
            table.AddRow(
                activity.OccuredOn!.Value.Format(),
                text);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}