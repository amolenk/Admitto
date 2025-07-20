using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email.Test;

public class TestEmailSettings : TeamEventSettings
{
    [CommandOption("--recipient")]
    public required string RecipientEmail { get; init; }
}

public abstract class TestEmailCommand<TSettings>(
    EmailType emailType,
    IAccessTokenProvider accessTokenProvider,
    IConfiguration configuration)
    : ApiCommand<TSettings>(accessTokenProvider, configuration)
    where TSettings : TestEmailSettings
{
    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var recipientEmail = string.IsNullOrWhiteSpace(settings.RecipientEmail)
            ? await GetTeamRecipientEmail(teamSlug)
            : settings.RecipientEmail;
        
        var request = new SendEmailRequest
        {
            EmailType = emailType,
            DataEntityId = GetDataEntityId(settings),
            RecipientEmail = recipientEmail
        };

        var succes = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Email.PostAsync(request));
        if (!succes) return 1;
        
        AnsiConsole.MarkupLine($"[green]✓ Successfully enqueued test e-mail for {recipientEmail}.[/]");
        return 0;
    }
    
    protected abstract Guid GetDataEntityId(TSettings settings);

    private async ValueTask<string> GetTeamRecipientEmail(string teamSlug)
    {
        var response = await CallApiAsync(async client => await client.Teams[teamSlug].GetAsync());
        if (response is null)
        {
            throw new InvalidOperationException("Failed to retrieve team information.");
        }

        return response.EmailSettings!.SenderEmail!;
    }
}