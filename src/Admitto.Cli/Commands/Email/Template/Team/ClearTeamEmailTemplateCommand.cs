using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Team;

public class ClearTeamEmailTemplateSettings : TeamSettings
{
    [CommandOption("--type")]
    [Description("The type of email template to clear")]
    public string? EmailType { get; init; }

    public override ValidationResult Validate()
    {
        return EmailType is null ? ValidationErrors.EmailTypeMissing : base.Validate();
    }
}

public class ClearTeamEmailTemplateCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<ClearTeamEmailTemplateSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ClearTeamEmailTemplateSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        
        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].EmailTemplates[settings.EmailType] .DeleteAsync());
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully cleared team-level template for '{settings.EmailType}' emails.");
        return 0;
    }
}