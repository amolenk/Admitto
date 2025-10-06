namespace Amolenk.Admitto.Cli.Commands.Email.Verification;

public class RequestOtpCodeSettings : TeamEventSettings
{
    [CommandOption("--email")]
    public string? Email { get; set; }
    
    public override ValidationResult Validate()
    {
        if (Email is null)
        {
            return ValidationErrors.EmailMissing;
        }

        return base.Validate();
    }
}

public class RequestOtpCodeCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration, OutputService outputService)
    : ApiCommand<RequestOtpCodeSettings>(accessTokenProvider, configuration, outputService)
{
    public override async Task<int> ExecuteAsync(CommandContext context, RequestOtpCodeSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new RequestOtpCodeRequest
        {
            Email = settings.Email
        };

        var success = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Public.Otp.PostAsync(request));
        if (!success) return 1;

        OutputService.WriteSuccesMessage($"Successfully requested OTP code for '{settings.Email}'.");
        return 0;
    }
}