using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Development;

// TODO Don't think this is something we need to support in the CLI

public class VerifyAttendeeSettings : TeamEventSettings
{
    [CommandOption("-a|--attendeeId")]
    public Guid RegistrationId { get; init; }

    [CommandOption("--code")]
    public string? Code { get; set; } = null!;
}

public class VerifyAttendeeCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<VerifyAttendeeSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, VerifyAttendeeSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new VerifyAttendeeRequest
        {
            Code = settings.Code
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees[settings.RegistrationId].Verify.PutAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully verified attendee.[/]");
        return 0;
    }
}