using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;
using ClosedXML.Excel;

namespace Amolenk.Admitto.Cli.Commands.Email.RecipientLists;

public class AddEmailRecipientListSettings : TeamEventSettings
{
    [CommandOption("--name")]
    [Description("The name of the list")]
    public string? ListName { get; init; }

    [CommandOption("--in")]
    [Description("The path of the Excel input file")]
    public string? InputPath { get; set; }

    public override ValidationResult Validate()
    {
        if (ListName is null)
        {
            return ValidationErrors.NameMissing;
        }

        if (string.IsNullOrWhiteSpace(InputPath))
        {
            return ValidationErrors.InputPathMissing;
        }

        if (!File.Exists(InputPath))
        {
            return ValidationErrors.InputPathDoesNotExist;
        }

        return base.Validate();
    }
}

public class AddEmailRecipientListCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<AddEmailRecipientListSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AddEmailRecipientListSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = CreateRequest(settings.ListName!, settings.InputPath!);

        var result = await admittoService.SendAsync(client =>
            client.AddEmailRecipientListAsync(teamSlug, eventSlug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage(
            $"Successfully added {settings.ListName} recipient list with {request.Recipients.Count} email(s).");
        return 0;
    }

    private static AddEmailRecipientListRequest CreateRequest(string listName, string inputPath)
    {
        using var workbook = new XLWorkbook(inputPath);

        var worksheet = workbook.Worksheets.First();

        var table = worksheet.Tables.FirstOrDefault();
        if (table is null)
        {
            throw new InvalidOperationException(
                "The input Excel file must include a table with at least an email address for each recipient.");
        }

        if (!table.ShowHeaderRow)
        {
            throw new InvalidOperationException(
                "The Excel table must include a header row.");
        }

        var columnNames = table.Row(1).Cells().Select(c => c.GetString()).ToList();
        if (!columnNames.Any(c => c.Equals("email", StringComparison.InvariantCultureIgnoreCase)))
        {
            throw new InvalidOperationException(
                "The Excel table must include an email column containing the email address for each recipient.");
        }

        return new AddEmailRecipientListRequest
        {
            Name = listName,
            Recipients = table.Rows().Skip(1)
                .Select(r =>
                {
                    var recipient = new EmailRecipientDto
                    {
                        Details = []
                    };

                    for (var i = 0; i < r.CellCount(); i++)
                    {
                        var columnName = columnNames[i];
                        var cellValue = r.Cell(i + 1).GetString();

                        if (string.Equals(columnName, "email", StringComparison.InvariantCultureIgnoreCase))
                        {
                            recipient.Email = cellValue;
                        }
                        else
                        {
                            recipient.Details.Add(
                                new EmailRecipientDetailDto
                                {
                                    Name = columnName,
                                    Value = cellValue
                                });
                        }
                    }

                    return recipient;
                })
                .ToList()
        };
    }
}