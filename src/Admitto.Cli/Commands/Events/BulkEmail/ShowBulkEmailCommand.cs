using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.BulkEmail;

public class ShowBulkEmailSettings : TeamEventSettings
{
    [CommandOption("--id <jobId>")]
    [Description("Bulk-email job ID (GUID).")]
    public Guid? JobId { get; init; }

    public override ValidationResult Validate()
    {
        if (!JobId.HasValue || JobId.Value == Guid.Empty)
        {
            return ValidationErrors.IdMissing;
        }

        return base.Validate();
    }
}

public class ShowBulkEmailCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<ShowBulkEmailSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context, ShowBulkEmailSettings settings, CancellationToken cancellationToken)
    {
        // Resolve slugs eagerly so they are validated before the API call.
        _ = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        _ = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var dto = await admittoService.QueryAsync(
            client => client.GetBulkEmailAsync(settings.JobId!.Value, cancellationToken));

        if (dto is null) return 1;

        var grid = new Grid().AddColumn().AddColumn();
        grid.AddRow("[bold]ID[/]", dto.Id.ToString());
        grid.AddRow("[bold]Email type[/]", dto.EmailType ?? "-");
        grid.AddRow("[bold]Status[/]", dto.Status.ToString().Humanize());
        grid.AddRow("[bold]Recipients[/]", dto.RecipientCount.ToString());
        grid.AddRow("[bold]Sent[/]", dto.SentCount.ToString());
        grid.AddRow("[bold]Failed[/]", dto.FailedCount.ToString());
        grid.AddRow("[bold]Cancelled[/]", dto.CancelledCount.ToString());
        grid.AddRow("[bold]Triggered by[/]", dto.IsSystemTriggered ? "system" : (dto.TriggeredBy ?? "-"));
        grid.AddRow("[bold]Created[/]", dto.CreatedAt.Format());
        grid.AddRow("[bold]Started[/]", dto.StartedAt?.Format() ?? "-");
        grid.AddRow("[bold]Completed[/]", dto.CompletedAt?.Format() ?? "-");
        grid.AddRow("[bold]Cancel requested[/]", dto.CancellationRequestedAt?.Format() ?? "-");
        grid.AddRow("[bold]Cancelled at[/]", dto.CancelledAt?.Format() ?? "-");
        grid.AddRow("[bold]Last error[/]", dto.LastError ?? "-");
        grid.AddRow("[bold]Version[/]", dto.Version.ToString());

        AnsiConsole.Write(grid);

        if (dto.Source is not null)
        {
            var sourceJson = JsonSerializer.Serialize(dto.Source, new JsonSerializerOptions { WriteIndented = true });
            AnsiConsole.Write(new Panel(new JsonText(sourceJson)).Header("Source"));
        }

        if (dto.Recipients is { Count: > 0 })
        {
            var table = new Table();
            table.AddColumn("Email");
            table.AddColumn("Display name");
            table.AddColumn("Status");
            table.AddColumn("Last error");

            foreach (var r in dto.Recipients)
            {
                table.AddRow(
                    r.Email ?? "-",
                    r.DisplayName ?? "-",
                    r.Status.ToString().Humanize(),
                    r.LastError ?? "-");
            }

            AnsiConsole.Write(table);
        }

        return 0;
    }
}
