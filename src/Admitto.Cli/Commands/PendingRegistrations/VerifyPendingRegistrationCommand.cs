using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.PendingRegistrations;

public class VerifyPendingRegistrationSettings : PendingRegistrationSettings
{
    [CommandOption("-r|--registrationId")]
    public Guid RegistrationId { get; init; }

    [CommandOption("--code")]
    public string? Code { get; set; } = null!;
}

public class VerifyPendingRegistrationCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : PendingRegistrationCommandBase<VerifyPendingRegistrationSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, VerifyPendingRegistrationSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new VerifyPendingRegistrationRequest
        {
            Code = settings.Code
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Registrations[settings.RegistrationId].Verify.PutAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully verified pending registration.[/]");
        return 0;
    }
}