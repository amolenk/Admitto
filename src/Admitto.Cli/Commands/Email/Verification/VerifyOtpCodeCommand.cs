namespace Amolenk.Admitto.Cli.Commands.Email.Verification;

public class VerifyOtpCodeSettings : TeamEventSettings
{
    [CommandOption("--email")]
    public string? Email { get; set; }

    [CommandOption("--code")]
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

public class VerifyOtpCodeCommand(
    IAccessTokenProvider accessTokenProvider, 
    IConfiguration configuration,
    OutputService outputService)
    : ApiCommand<VerifyOtpCodeSettings>(accessTokenProvider, configuration, outputService)
{
    public override async Task<int> ExecuteAsync(CommandContext context, VerifyOtpCodeSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new VerifyOtpCodeRequest
        {
            Email = settings.Email,
            Code = settings.Code
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Public.Verify.PostAsync(request));
        if (response is null) return 1;
        
        outputService.WriteSuccesMessage("Successfully verified email address.");
        outputService.WriteLine($"Token: {response.RegistrationToken}");
        return 0;
    }
}