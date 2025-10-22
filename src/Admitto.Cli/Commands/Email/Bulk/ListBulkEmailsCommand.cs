using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Email.Bulk;

public class ListBulkEmailsCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await apiService.CallApiAsync(async client => 
            await client.Teams[teamSlug].Events[eventSlug].Emails.Bulk.GetAsync());
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Email type");
        table.AddColumn("Repeat");
        table.AddColumn("Status");
        table.AddColumn("Last run");

        foreach (var bulkEmail in response.BulkEmails!
                     .OrderBy(be => be.EmailType)
                     .ThenByDescending(be => be.LastRunAt ?? DateTimeOffset.MinValue))
        {
            table.AddRow(
                $"[grey]{bulkEmail.Id}[/]",
                bulkEmail.EmailType!,
                FormatRepeat(bulkEmail.Repeat),
                bulkEmail.Status == BulkEmailWorkItemStatus.Error
                    ? $"[red]{bulkEmail.Error}[/]"
                    : bulkEmail.Status!.Value.ToString(),
                bulkEmail.LastRunAt?.ToString() ?? "Never");
        }

        AnsiConsole.Write(table);
        return 0;
    }
    
    private string FormatRepeat(BulkEmailWorkItemRepeatDto? repeat) =>
        repeat is null
            ? "-"
            : $"Between {repeat.WindowStart} and {repeat.WindowEnd}";
}