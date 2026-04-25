using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.BulkEmail;

public class StartBulkEmailSettings : BulkEmailSourceSettings
{
    [CommandOption("--type")]
    [Description("Email type. Use a saved template type (e.g. 'ticket', 'reconfirm') or 'bulk-custom' for ad-hoc content.")]
    public string? EmailType { get; init; }

    [CommandOption("--subject")]
    [Description("Ad-hoc subject (only for --type bulk-custom). Required when type is bulk-custom.")]
    public string? Subject { get; init; }

    [CommandOption("--text-body")]
    [Description("Ad-hoc plain-text body literal, or '@file.txt' to load from a file (only for --type bulk-custom).")]
    public string? TextBody { get; init; }

    [CommandOption("--html-body")]
    [Description("Ad-hoc HTML body literal, or '@file.html' to load from a file (only for --type bulk-custom).")]
    public string? HtmlBody { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(EmailType))
        {
            return ValidationErrors.EmailTypeMissing;
        }

        var isCustom = string.Equals(EmailType, "bulk-custom", StringComparison.OrdinalIgnoreCase);

        if (isCustom)
        {
            if (string.IsNullOrWhiteSpace(Subject))
            {
                return ValidationResult.Error("--subject is required when --type is 'bulk-custom'.");
            }

            if (string.IsNullOrWhiteSpace(TextBody) && string.IsNullOrWhiteSpace(HtmlBody))
            {
                return ValidationResult.Error("At least one of --text-body or --html-body is required when --type is 'bulk-custom'.");
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(Subject) ||
                !string.IsNullOrWhiteSpace(TextBody) ||
                !string.IsNullOrWhiteSpace(HtmlBody))
            {
                return ValidationResult.Error(
                    "--subject/--text-body/--html-body are only valid with --type 'bulk-custom'; saved templates supply their own content.");
            }
        }

        var sourceResult = ValidateSource();
        return sourceResult.Successful ? base.Validate() : sourceResult;
    }
}

public class StartBulkEmailCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<StartBulkEmailSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context, StartBulkEmailSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new CreateBulkEmailCliRequest
        {
            EmailType = settings.EmailType!,
            Subject = settings.Subject,
            TextBody = LoadOptionalContent(settings.TextBody),
            HtmlBody = LoadOptionalContent(settings.HtmlBody),
            Source = settings.BuildSource()
        };

        var response = await admittoService.QueryAsync(
            client => client.CreateBulkEmailV2Async(teamSlug, eventSlug, request, cancellationToken));

        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Bulk-email job created (ID: {response.BulkEmailJobId}).");
        return 0;
    }

    private static string? LoadOptionalContent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (value.StartsWith('@'))
        {
            var path = value[1..];
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Body file not found: {path}", path);
            }

            return File.ReadAllText(path);
        }

        return value;
    }
}
