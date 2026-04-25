using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.BulkEmail;

public class CancelBulkEmailSettings : TeamEventSettings
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

public class CancelBulkEmailCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<CancelBulkEmailSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context, CancelBulkEmailSettings settings, CancellationToken cancellationToken)
    {
        // Resolve slugs eagerly so they are validated before the API call.
        _ = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        _ = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var success = await admittoService.SendAsync(
            client => client.CancelBulkEmailAsync(settings.JobId!.Value, cancellationToken));

        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Cancellation requested for job {settings.JobId!.Value}.");
        return 0;
    }
}
