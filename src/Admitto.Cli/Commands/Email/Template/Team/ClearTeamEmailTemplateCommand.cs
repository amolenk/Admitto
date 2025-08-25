using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Team;

public class ClearTeamEmailTemplateSettings : TeamSettings
{
    [CommandOption("--emailType")]
    [EmailTypeDescription]
    public EmailType? EmailType { get; init; }

    public override ValidationResult Validate()
    {
        if (EmailType is null)
        {
            return ValidationErrors.EmailTypeMissing;
        }

        return base.Validate();
    }
}

public class ClearTeamEmailTemplateCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<ClearTeamEmailTemplateSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ClearTeamEmailTemplateSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].EmailTemplates[settings.EmailType.ToString()] .DeleteAsync());
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully cleared team-level template for '{settings.EmailType}' emails.[/]");
        return 0;
    }
}