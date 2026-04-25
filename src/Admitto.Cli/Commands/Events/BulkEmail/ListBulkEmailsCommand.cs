using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.BulkEmail;

public class ListBulkEmailsCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context, TeamEventSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await admittoService.QueryAsync(
            client => client.GetBulkEmailsAsync(teamSlug, eventSlug, cancellationToken));

        if (response is null) return 1;

        var rows = response.OrderByDescending(r => r.CreatedAt).ToList();

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Type");
        table.AddColumn("Status");
        table.AddColumn("Recipients");
        table.AddColumn("Sent");
        table.AddColumn("Failed");
        table.AddColumn("Cancelled");
        table.AddColumn("Triggered by");
        table.AddColumn("Created");

        foreach (var job in rows)
        {
            table.AddRow(
                job.Id.ToString()[..8],
                job.EmailType ?? "-",
                job.Status.ToString().Humanize(),
                job.RecipientCount.ToString(),
                job.SentCount.ToString(),
                job.FailedCount.ToString(),
                job.CancelledCount.ToString(),
                job.IsSystemTriggered ? "system" : (job.TriggeredBy ?? "-"),
                job.CreatedAt.Format());
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
