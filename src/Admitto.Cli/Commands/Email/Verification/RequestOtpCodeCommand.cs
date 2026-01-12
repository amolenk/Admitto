using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Email.Verification;

public class RequestOtpCodeSettings : TeamEventSettings
{
    [CommandOption("--email")]
    [Description("The email address to request the OTP code for")]
    public string? Email { get; set; }
    
    public override ValidationResult Validate()
    {
        return Email is null ? ValidationErrors.EmailMissing : base.Validate();
    }
}

public class RequestOtpCodeCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<RequestOtpCodeSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RequestOtpCodeSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new RequestOtpCodeRequest
        {
            Email = settings.Email
        };

        var result = await admittoService.SendAsync(client => client.RequestOtpAsync(
            teamSlug,
            eventSlug,
            request,
            cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully requested OTP code for '{settings.Email}'.");
        return 0;
    }
}