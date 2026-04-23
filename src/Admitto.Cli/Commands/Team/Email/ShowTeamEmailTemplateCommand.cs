using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Team.Email;

public class ShowTeamEmailTemplateSettings : TeamSettings
{
    [CommandOption("--type")]
    [Description("The type of email template (e.g. 'ticket')")]
    public string? EmailType { get; init; }

    public override ValidationResult Validate()
    {
        return EmailType is null ? ValidationErrors.EmailTypeMissing : base.Validate();
    }
}

public class ShowTeamEmailTemplateCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<ShowTeamEmailTemplateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ShowTeamEmailTemplateSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var response = await admittoService.QueryAsync(
            client => client.GetTeamEmailTemplateAsync(teamSlug, null, settings.EmailType!, cancellationToken));

        if (response is null)
        {
            AnsiConsoleExt.WriteErrorMessage($"No '{settings.EmailType}' template configured for this team.");
            return 1;
        }

        AnsiConsole.Write(new Rule($"'{settings.EmailType}' template for team '{teamSlug}'")
        {
            Justification = Justify.Left,
            Style = Style.Parse("cyan")
        });

        var grid = new Grid();
        grid.AddColumn(new GridColumn { Width = 12 });
        grid.AddColumn();

        grid.AddRow("Subject:", response.Subject);
        grid.AddRow("Version:", response.Version.ToString());

        AnsiConsole.Write(grid);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[cyan]HTML body:[/]");
        AnsiConsole.WriteLine(response.HtmlBody);

        return 0;
    }
}
