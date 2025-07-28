using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Event;

public class SetEventEmailTemplateSettings : TeamEventSettings
{
    [CommandOption("--emailType")]
    public EmailType? EmailType { get; set; }

    [CommandOption("--subject")]
    public required string Subject { get; set; }
    
    [CommandOption("--bodyPath")]
    public required string BodyPath { get; set; }
    
    public override ValidationResult Validate()
    {
        if (EmailType is null)
        {
            return ValidationResult.Error("Email type is required.");
        }

        if (string.IsNullOrWhiteSpace(Subject))
        {
            return ValidationResult.Error("Subject cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(BodyPath) || !File.Exists(BodyPath))
        {
            return ValidationResult.Error("Body path is required and must point to an existing file.");
        }
        
        return base.Validate();
    }
}

public class SetEventEmailTemplateCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<SetEventEmailTemplateSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetEventEmailTemplateSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new ConfigureEventEmailTemplateRequest
        {
            Subject = settings.Subject,
            Body = await File.ReadAllTextAsync(settings.BodyPath)
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Email.Templates[settings.EmailType.ToString()]
                .PutAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine($"[green]✓ Successfully set event-level template for '{settings.EmailType}' emails.[/]");
        return 0;
    }
}