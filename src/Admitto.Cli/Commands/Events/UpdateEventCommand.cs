using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events;

public class UpdateEventSettings : TeamEventSettings
{
    [CommandOption("--expected-version <version>")]
    [Description("The expected current version of the event (optimistic concurrency token)")]
    public uint? ExpectedVersion { get; init; }

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

    public override ValidationResult Validate()
    {
        return base.Validate();
    }
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

        var request = new UpdateEventRequest
        {
            ExpectedVersion = settings.ExpectedVersion,
            Name = settings.Name,
            WebsiteUrl = settings.WebsiteUrl,
            BaseUrl = settings.BaseUrl,
            StartsAt = settings.StartsAt,
            EndsAt = settings.EndsAt
        };

        var success = await admittoService.SendAsync(
            client => client.UpdateTicketedEventAsync(teamSlug, eventSlug, request, cancellationToken));

        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully updated event '{eventSlug}'.");
        return 0;
    }
}
