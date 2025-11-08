using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class ShowSettings : TeamEventSettings
{
    [CommandOption("--id")]
    [Description("The id of the attendee")]
    public Guid? Id { get; set; }

    public override ValidationResult Validate()
    {
        return Id is null ? ValidationErrors.IdMissing : base.Validate();
    }
    
    // [CommandOption("--qrCodeOutputPath")]
    // public string? QRCodeFilePath { get; set; }
}

public class ShowAttendeeCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<ShowSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ShowSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);
        
        var attendeeResponse = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees[settings.Id!.Value].GetAsync());
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