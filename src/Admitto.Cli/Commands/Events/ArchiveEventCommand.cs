using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events;

public class ArchiveEventSettings : TeamEventSettings
{
    [CommandOption("--expected-version <version>")]
    [Description("The expected current version of the event (optimistic concurrency token)")]
    public int? ExpectedVersion { get; init; }

    public override ValidationResult Validate()
    {
        return base.Validate();
    }
}

public class ArchiveEventCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<ArchiveEventSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ArchiveEventSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new ArchiveTicketedEventHttpRequest
        {
            ExpectedVersion = settings.ExpectedVersion
        };

        var success = await admittoService.SendAsync(
            client => client.ArchiveTicketedEventAsync(teamSlug, eventSlug, request, cancellationToken));

        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully archived event '{eventSlug}'.");
        return 0;
    }
}
