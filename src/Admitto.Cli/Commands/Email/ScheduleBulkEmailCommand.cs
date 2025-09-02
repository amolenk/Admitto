using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email;

public class PreviewEmailSettings : TeamEventSettings
{
    [CommandOption("--type")]
    public string? EmailType { get; init; }
    
    public override ValidationResult Validate()
    {
        if (EmailType is null)
        {
            return ValidationErrors.EmailTypeMissing;
        }

        return base.Validate();
    }
}

public class ScheduleBulkEmailCommand(
    IAccessTokenProvider accessTokenProvider,
    IConfiguration configuration)
    : ApiCommand<PreviewEmailSettings>(accessTokenProvider, configuration)
{
    public sealed override async Task<int> ExecuteAsync(CommandContext context, PreviewEmailSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new ScheduleBulkEmailRequest
        {
            EmailType = settings.EmailType,
            EarliestSendTime = DateTimeOffset.MinValue,
            LatestSendTime = DateTimeOffset.MaxValue
        };
  
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Emails.Bulk.PostAsync(request));
        if (!response) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully submitted '{settings.EmailType}' email bulk.[/]");
        
        return 0;
    }
}

