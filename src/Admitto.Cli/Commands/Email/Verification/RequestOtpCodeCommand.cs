using Amolenk.Admitto.Cli.Common;

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

public class RequestOtpCodeCommand(IApiService apiService, IConfigService configService)
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

        var success = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Public.Otp.PostAsync(request));
        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully requested OTP code for '{settings.Email}'.");
        return 0;
    }
}