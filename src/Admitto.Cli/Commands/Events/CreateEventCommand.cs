using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events;

public class CreateEventSettings : TeamSettings
{
    [CommandOption("-s|--slug")]
    [Description("Slug of the event to create (e.g. 'my-cool-event')")]
    public string? EventSlug { get; init; }

    [CommandOption("-n|--name")]
    [Description("The name of the event")]
    public string? Name { get; init; }

    [CommandOption("--website")]
    [Description("The website of the event")]
    public string? Website { get; init; }

    [CommandOption("--start")]
    [Description("The start date and time of the event.")]
    public DateTimeOffset? StartsAt { get; init; }

    [CommandOption("--end")]
    [Description("The end date and time of the event.")]
    public DateTimeOffset? EndsAt { get; init; }

    [CommandOption("--baseUrl")]
    [Description("The base URL for event links (e.g. qr-codes, cancellations, etc.)")]
    public string? BaseUrl { get; init; }

    [CommandOption("--wait")]
    [Description("Wait for the asynchronous creation request to reach a terminal status before returning.")]
    public bool Wait { get; init; }

    [CommandOption("--wait-timeout")]
    [Description("Maximum time (in seconds) to wait when --wait is specified. Defaults to 60.")]
    public int WaitTimeoutSeconds { get; init; } = 60;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return ValidationErrors.EventNameMissing;
        }

        if (string.IsNullOrWhiteSpace(EventSlug))
        {
            return ValidationErrors.EventSlugMissing;
        }

        if (string.IsNullOrWhiteSpace(Website))
        {
            return ValidationErrors.EventWebsiteMissing;
        }

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            return ValidationErrors.EventBaseUrlMissing;
        }

        if (!StartsAt.HasValue)
        {
            return ValidationErrors.EventStartsAtMissing;
        }

        if (!EndsAt.HasValue)
        {
            return ValidationErrors.EventEndsAtMissing;
        }

        return base.Validate();
    }
}

public class CreateEventCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<CreateEventSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CreateEventSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var request = new RequestTicketedEventCreationHttpRequest
        {
            Slug = settings.EventSlug!.Kebaberize(),
            Name = settings.Name!,
            WebsiteUrl = settings.Website!,
            BaseUrl = settings.BaseUrl!,
            StartsAt = settings.StartsAt!.Value,
            EndsAt = settings.EndsAt!.Value
        };

        var creationRequestId = await admittoService.QueryAsync<Guid>(async client =>
        {
            await client.RequestTicketedEventCreationAsync(teamSlug, request, cancellationToken);

            var location = client.LastResponseLocation?.ToString()
                ?? throw new InvalidOperationException(
                    "Server accepted the request but did not return a Location header.");

            var lastSlash = location.LastIndexOf('/');
            if (lastSlash < 0 || lastSlash == location.Length - 1
                || !Guid.TryParse(location[(lastSlash + 1)..], out var id))
            {
                throw new InvalidOperationException(
                    $"Server returned a Location header in an unexpected format: '{location}'.");
            }

            return id;
        });

        if (creationRequestId == Guid.Empty) return 1;

        AnsiConsoleExt.WriteSuccesMessage(
            $"Submitted creation request {creationRequestId} for event '{settings.EventSlug}'.");

        if (!settings.Wait)
        {
            return 0;
        }

        return await WaitForCompletionAsync(teamSlug, creationRequestId, settings, cancellationToken);
    }

    private async Task<int> WaitForCompletionAsync(
        string teamSlug,
        Guid creationRequestId,
        CreateEventSettings settings,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(settings.WaitTimeoutSeconds);
        var pollInterval = TimeSpan.FromSeconds(1);

        while (true)
        {
            var status = await admittoService.QueryAsync(client =>
                client.GetEventCreationRequestAsync(teamSlug, creationRequestId, cancellationToken));

            if (status is null) return 1;

            if (!string.Equals(status.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(status.Status, "Created", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsoleExt.WriteSuccesMessage(
                        $"Event '{settings.EventSlug}' was created (event id: {status.TicketedEventId}).");
                    return 0;
                }

                var reason = string.IsNullOrWhiteSpace(status.RejectionReason)
                    ? status.Status
                    : $"{status.Status}: {status.RejectionReason}";
                AnsiConsoleExt.WriteErrorMessage($"Event creation did not succeed ({reason}).");
                return 1;
            }

            if (DateTimeOffset.UtcNow >= deadline)
            {
                AnsiConsoleExt.WriteErrorMessage(
                    $"Timed out after {settings.WaitTimeoutSeconds}s waiting for event creation to complete.");
                return 1;
            }

            await Task.Delay(pollInterval, cancellationToken);
        }
    }
}
