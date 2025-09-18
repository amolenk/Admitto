namespace Amolenk.Admitto.Cli.Commands.Email.Template.Event;

public class ClearEventEmailTemplateSettings : TeamEventSettings
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

public class ClearEventEmailTemplateCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<ClearEventEmailTemplateSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ClearEventEmailTemplateSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].EmailTemplates[settings.EmailType]
                .DeleteAsync());
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully cleared event-level template for '{settings.EmailType}' emails.[/]");
        return 0;
    }
}