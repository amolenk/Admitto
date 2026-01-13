using System.Data;
using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;
using ClosedXML.Excel;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class ExportSettings : TeamEventSettings
{
    [CommandOption("--out")] 
    [Description("The path of the Excel output file")]
    public string? OutputPath { get; set; }

    public override ValidationResult Validate()
    {
        return OutputPath is null ? ValidationErrors.OutputPathMissing : base.Validate();
    }
}

public class ExportAttendeesCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<ExportSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ExportSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var attendeesResponse = await admittoService.QueryAsync(client =>
            client.GetAttendeesAsync(teamSlug, eventSlug, cancellationToken));
        if (attendeesResponse is null) return 1;

        var eventResponse = await admittoService.QueryAsync(client =>
            client.GetTicketedEventAsync(teamSlug, eventSlug, cancellationToken));
        if (eventResponse is null) return 1;

        using var workbook = new XLWorkbook();

        var attendeesData = CreateAttendeesDataTable(
            attendeesResponse.Attendees,
            eventResponse.AdditionalDetailSchemas!);
        var attendeesSheet = workbook.AddWorksheet();
        attendeesSheet.Name = attendeesData.TableName;
        attendeesSheet.ColumnWidth = 12;
        attendeesSheet.FirstCell().InsertTable(attendeesData, attendeesData.TableName, true);

        var ticketsData = CreateTicketsDataTable(
            attendeesResponse.Attendees,
            eventResponse.AdditionalDetailSchemas!);
        var ticketsSheet = workbook.AddWorksheet();
        ticketsSheet.Name = ticketsData.TableName;
        ticketsSheet.ColumnWidth = 12;
        ticketsSheet.FirstCell().InsertTable(ticketsData, ticketsData.TableName, true);

        var totalsData = CreateTotalsDataTable(attendeesResponse.Attendees);
        var totalsSheet = workbook.AddWorksheet();
        totalsSheet.Name = totalsData.TableName;
        totalsSheet.ColumnWidth = 12;
        totalsSheet.FirstCell().InsertTable(totalsData, totalsData.TableName, true);

        workbook.SaveAs(settings.OutputPath);
        return 0;
    }

    private static DataTable CreateAttendeesDataTable(
        ICollection<AttendeeDto> attendees,
        ICollection<AdditionalDetailSchemaDto> additionalDetailSchemas)
    {
        var table = new DataTable("Attendees");
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("FirstName", typeof(string));
        table.Columns.Add("LastName", typeof(string));

        foreach (var additionalDetailSchema in additionalDetailSchemas)
        {
            table.Columns.Add(additionalDetailSchema.Name, typeof(string));
        }

        table.Columns.Add("Status", typeof(string));
        table.Columns.Add("LastChangedAt", typeof(DateTimeOffset));

        foreach (var attendee in attendees)
        {
            var row = table.NewRow();
            PopulateAttendeesRow(row, attendee, additionalDetailSchemas);
            table.Rows.Add(row);
        }

        return table;
    }

    private static DataTable CreateTicketsDataTable(
        ICollection<AttendeeDto> attendees,
        ICollection<AdditionalDetailSchemaDto> additionalDetailSchemas)
    {
        var table = new DataTable("Tickets");
        table.Columns.Add("TicketType", typeof(string));
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("FirstName", typeof(string));
        table.Columns.Add("LastName", typeof(string));

        foreach (var additionalDetailSchema in additionalDetailSchemas)
        {
            table.Columns.Add(additionalDetailSchema.Name, typeof(string));
        }

        table.Columns.Add("Status", typeof(string));
        table.Columns.Add("LastChangedAt", typeof(DateTimeOffset));

        foreach (var attendee in attendees.Where(a => a.Status != RegistrationStatus.Canceled))
        {
            foreach (var ticket in attendee.Tickets!)
            {
                var row = table.NewRow();
                PopulateAttendeesRow(row, attendee, additionalDetailSchemas);
                row["TicketType"] = ticket.TicketTypeSlug;
                table.Rows.Add(row);
            }
        }

        return table;
    }
    
    private static DataTable CreateTotalsDataTable(ICollection<AttendeeDto> attendees)
    {
        var table = new DataTable("Totals");
        table.Columns.Add("TicketType", typeof(string));
        table.Columns.Add("RegistrationCount", typeof(int));
        table.Columns.Add("ReconfirmedCount", typeof(int));
        table.Columns.Add("CheckedInCount", typeof(int));
        table.Columns.Add("NoShowCount", typeof(int));

        var totals = attendees
            .Where(a => a.Status != RegistrationStatus.Canceled)
            .SelectMany(a => a.Tickets.Select(t => new
            {
                a.Status,
                t.TicketTypeSlug,
                t.Quantity
            }))
            .GroupBy(t => t.TicketTypeSlug)
            .Select(g => new
            {
                TicketType = g.Key,
                RegistrationCount = g.Sum(t => t.Quantity),
                ReconfirmedCount = g.Count(x => x.Status == RegistrationStatus.Reconfirmed),
                CheckedInCount = g.Count(x => x.Status == RegistrationStatus.CheckedIn),
                NoShowCount = g.Count(x => x.Status == RegistrationStatus.NoShow),
            });
        
        foreach (var total in totals)
        {
            var row = table.NewRow();
            row["TicketType"] = total.TicketType;
            row["RegistrationCount"] = total.RegistrationCount;
            row["ReconfirmedCount"] = total.ReconfirmedCount;
            row["CheckedInCount"] = total.CheckedInCount;
            row["NoShowCount"] = total.NoShowCount;
            table.Rows.Add(row);
        }

        return table;
    }

    private static void PopulateAttendeesRow(
        DataRow row,
        AttendeeDto attendee,
        ICollection<AdditionalDetailSchemaDto> additionalDetailSchemas)
    {
        row["Email"] = attendee.Email;
        row["FirstName"] = attendee.FirstName;
        row["LastName"] = attendee.LastName;

        foreach (var additionalDetailSchema in additionalDetailSchemas)
        {
            var detail = attendee.AdditionalDetails!.FirstOrDefault(ad => ad.Name == additionalDetailSchema.Name);

            if (detail is not null)
            {
                row[additionalDetailSchema.Name!] = detail.Value;
            }
        }

        row["Status"] = attendee.Status;
        row["LastChangedAt"] = attendee.LastChangedAt.ToLocalTime();
    }
}