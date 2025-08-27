using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class CancelSettings : TeamEventSettings
{
    [CommandOption("-a|--attendeeId")]
    public Guid? RegistrationId { get; set; }

    public override ValidationResult Validate()
    {
        if (RegistrationId is null)
        {
            return ValidationErrors.RegistrationIdMissing;
        }

        return base.Validate();
    }
}

public class CancelCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<ShowSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ShowSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        // Get signature required to cancel the registration
        var signature = await GetSignatureAsync(teamSlug, eventSlug, settings.RegistrationId!.Value);
        if (signature is null) return 1;
        
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].AttendeeRegistrations[settings.RegistrationId!.Value].DeleteAsync(c =>
            {
                c.QueryParameters.Signature = signature;
            }));
        if (response is null) return 1;
        
        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully cancelled registration.[/]");
        return 0;
    }

    private async ValueTask<string?> GetSignatureAsync(string teamSlug, string eventSlug, Guid registrationId)
    {
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].AttendeeRegistrations[registrationId].GetAsync());
        
        return response?.Signature;
    }
}