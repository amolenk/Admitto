using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Team.Email;

public class UpdateTeamEmailSettings : TeamSettings
{
    [CommandOption("--smtp-host")]
    [Description("SMTP server host name")]
    public string? SmtpHost { get; init; }

    [CommandOption("--smtp-port")]
    [Description("SMTP server port")]
    public int? SmtpPort { get; init; }

    [CommandOption("--from-address")]
    [Description("From address used for outgoing emails")]
    public string? FromAddress { get; init; }

    [CommandOption("--auth-mode")]
    [Description("Authentication mode: 'none' or 'basic'")]
    public string? AuthMode { get; init; }

    [CommandOption("--username")]
    [Description("SMTP username (required for basic auth)")]
    public string? Username { get; init; }

    [CommandOption("--password")]
    [Description("SMTP password (required for basic auth)")]
    public string? Password { get; init; }

    [CommandOption("--expected-version <version>")]
    [Description("The expected current version of the email settings (optimistic concurrency token). Omit when creating.")]
    public int? ExpectedVersion { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(SmtpHost))
        {
            return ValidationResult.Error("Missing required option --smtp-host.");
        }

        if (!SmtpPort.HasValue)
        {
            return ValidationResult.Error("Missing required option --smtp-port.");
        }

        if (string.IsNullOrWhiteSpace(FromAddress))
        {
            return ValidationResult.Error("Missing required option --from-address.");
        }

        if (string.IsNullOrWhiteSpace(AuthMode))
        {
            return ValidationResult.Error("Missing required option --auth-mode (use 'none' or 'basic').");
        }

        if (!Enum.TryParse<EmailAuthMode>(AuthMode, ignoreCase: true, out _))
        {
            return ValidationResult.Error("Invalid --auth-mode value. Use 'none' or 'basic'.");
        }

        return base.Validate();
    }
}

public class UpdateTeamEmailCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<UpdateTeamEmailSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpdateTeamEmailSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var authMode = Enum.Parse<EmailAuthMode>(settings.AuthMode!, ignoreCase: true);

        var request = new UpsertEmailSettingsHttpRequest
        {
            SmtpHost = settings.SmtpHost!,
            SmtpPort = settings.SmtpPort!.Value,
            FromAddress = settings.FromAddress!,
            AuthMode = authMode,
            Username = settings.Username,
            Password = settings.Password,
            Version = settings.ExpectedVersion
        };

        var success = await admittoService.SendAsync(
            client => client.UpsertTeamEmailSettingsAsync(teamSlug, null, request, cancellationToken));

        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully updated email settings for team '{teamSlug}'.");
        return 0;
    }
}
