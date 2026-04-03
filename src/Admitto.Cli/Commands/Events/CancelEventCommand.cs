using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events;

public class CancelEventSettings : TeamEventSettings
{
    [CommandOption("--expected-version <version>")]
    [Description("The expected current version of the event (optimistic concurrency token)")]
    public uint? ExpectedVersion { get; init; }

    public override ValidationResult Validate()
    {
        return base.Validate();
    }
}

public class CancelEventCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<CancelEventSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CancelEventSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new CancelTicketedEventRequest
        {
            ExpectedVersion = settings.ExpectedVersion
        };

        var success = await admittoService.SendAsync(
            client => client.CancelTicketedEventAsync(teamSlug, eventSlug, request, cancellationToken));

        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully cancelled event '{eventSlug}'.");
        return 0;
    }
}
