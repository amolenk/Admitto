using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

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

public class ClearTeamEmailTemplateCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<ClearTeamEmailTemplateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ClearTeamEmailTemplateSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var result = await admittoService.SendAsync(client =>
            client.ClearTeamEmailTemplateAsync(teamSlug, settings.EmailType, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage(
            $"Successfully cleared team-level template for '{settings.EmailType}' emails.");
        return 0;
    }
}