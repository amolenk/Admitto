using Humanizer;

namespace Amolenk.Admitto.Cli.Commands.Email.Bulk;

public class ListCommand(
    IAccessTokenProvider accessTokenProvider, 
    IConfiguration configuration,
    OutputService outputService)
    : ApiCommand<TeamEventSettings>(accessTokenProvider, configuration, outputService)
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var response = await CallApiAsync(async client => 
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

        outputService.Write(table);
        return 0;
    }
    
    private string FormatRepeat(BulkEmailWorkItemRepeatDto? repeat) =>
        repeat is null
            ? "-"
            : $"Between {repeat.WindowStart} and {repeat.WindowEnd}";
}