using Amolenk.Admitto.Cli.Commands.Email.Template.Event;
using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Team;

public class ClearTeamEmailTemplateSettings : TeamSettings
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

public class ClearTeamEmailTemplateCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<SetEventEmailTemplateSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetEventEmailTemplateSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Email.Templates[settings.EmailType.ToString()] .DeleteAsync());
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully cleared team-level template for '{settings.EmailType}' emails.[/]");
        return 0;
    }
}