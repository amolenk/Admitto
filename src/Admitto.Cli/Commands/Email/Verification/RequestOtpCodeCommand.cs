using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email.Verification;

public class RequestOtpCodeSettings : TeamEventSettings
{
    [CommandOption("--email")]
    public string? Email { get; set; }
}

public class RequestOtpCodeCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<RequestOtpCodeSettings>(accessTokenProvider, configuration)
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
            await client.Teams[teamSlug].Events[eventSlug].EmailVerification.Requests.PostAsync(request));
        if (!success) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully requested OTP code for '{settings.Email}'.[/]");
        return 0;
    }
}