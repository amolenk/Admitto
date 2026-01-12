using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Email.Verification;

public class VerifyOtpCodeSettings : TeamEventSettings
{
    [CommandOption("--email")]
    [Description("The email address to verify")]
    public string? Email { get; set; }

    [CommandOption("--code")]
    [Description("The OTP verification code")]
    public string? Code { get; set; } = null!;

    public override ValidationResult Validate()
    {
        if (Email is null)
        {
            return ValidationErrors.EmailMissing;
        }

        if (Code is null)
        {
            return ValidationErrors.EmailVerificationCodeMissing;
        }

        return base.Validate();
    }
}

public class VerifyOtpCodeCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<VerifyOtpCodeSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        VerifyOtpCodeSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new VerifyOtpCodeRequest
        {
            Email = settings.Email,
            Code = settings.Code
        };

        var response = await admittoService.QueryAsync(client =>
            client.VerifyOtpCodeAsync(teamSlug, eventSlug, request, cancellationToken));
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully verified email address.");
        AnsiConsoleExt.WriteSuccesMessage($"Token: {response.RegistrationToken}");
        return 0;
    }
}