using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Event;

public class ListEventEmailTemplatesCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        TeamEventSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await admittoService.QueryAsync(client =>
            client.GetEventEmailTemplatesAsync(teamSlug, eventSlug, cancellationToken));
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Email type");
        table.AddColumn("Status");

        foreach (var emailTemplate in response.EmailTemplates ?? [])
        {
            var status = emailTemplate.IsCustom
                ? emailTemplate.TicketedEventId is null ? "Custom (team-level)" : "Custom (event-level)"
                : "[grey]Default[/]";

            table.AddRow(emailTemplate.Type!, status);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}