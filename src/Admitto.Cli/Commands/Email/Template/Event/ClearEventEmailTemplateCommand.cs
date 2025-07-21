using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Event;

public class ClearEventEmailTemplateSettings : TeamEventSettings
{
    [CommandOption("--emailType")]
    public EmailType? EmailType { get; set; }

    public override ValidationResult Validate()
    {
        if (EmailType is null)
        {
            return ValidationResult.Error("Email type is required.");
        }

        return base.Validate();
    }
}

public class ClearEventEmailTemplateCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<SetEventEmailTemplateSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetEventEmailTemplateSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Email.Templates[settings.EmailType.ToString()]
                .DeleteAsync());
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully cleared event-level template for '{settings.EmailType}' emails.[/]");
        return 0;
    }
}