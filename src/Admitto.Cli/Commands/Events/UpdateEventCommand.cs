using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events;

public class UpdateEventSettings : TeamEventSettings
{
    [CommandOption("--expected-version <version>")]
    [Description("The expected current version of the event (optimistic concurrency token)")]
    public int? ExpectedVersion { get; init; }

    [CommandOption("-n|--name")]
    [Description("New name of the event")]
    public string? Name { get; init; }

    [CommandOption("--website")]
    [Description("New website URL of the event")]
    public string? WebsiteUrl { get; init; }

    [CommandOption("--baseUrl")]
    [Description("New base URL for event links")]
    public string? BaseUrl { get; init; }

    [CommandOption("--start")]
    [Description("New start date and time of the event")]
    public DateTimeOffset? StartsAt { get; init; }

    [CommandOption("--end")]
    [Description("New end date and time of the event")]
    public DateTimeOffset? EndsAt { get; init; }
}

public class UpdateEventCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<UpdateEventSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpdateEventSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        // The backend update endpoint requires the full event details. Fetch the current
        // values first and merge any user-provided overrides on top.
        var current = await admittoService.QueryAsync(client =>
            client.GetTicketedEventDetailsAsync(teamSlug, eventSlug, cancellationToken));

        if (current is null) return 1;

        var request = new UpdateTicketedEventDetailsHttpRequest
        {
            ExpectedVersion = settings.ExpectedVersion ?? current.Version,
            Name = settings.Name ?? current.Name,
            WebsiteUrl = settings.WebsiteUrl ?? current.WebsiteUrl,
            BaseUrl = settings.BaseUrl ?? current.BaseUrl,
            StartsAt = settings.StartsAt ?? current.StartsAt,
            EndsAt = settings.EndsAt ?? current.EndsAt
        };

        var success = await admittoService.SendAsync(
            client => client.UpdateTicketedEventDetailsAsync(teamSlug, eventSlug, request, cancellationToken));

        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully updated event '{eventSlug}'.");
        return 0;
    }
}
