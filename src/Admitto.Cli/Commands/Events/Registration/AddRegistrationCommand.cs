using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Registration;

public class AddRegistrationSettings : TeamEventSettings
{
    [CommandOption("--email")]
    [Description("The email address of the attendee")]
    public string? Email { get; init; }

    [CommandOption("--ticket")]
    [Description("Ticket type slug to register for (can be specified multiple times)")]
    public string[]? TicketTypeSlugs { get; init; }

    [CommandOption("--detail")]
    [Description("Additional detail in key=value form (can be specified multiple times)")]
    public string[]? AdditionalDetails { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return ValidationErrors.EmailMissing;

        if (TicketTypeSlugs is null || TicketTypeSlugs.Length == 0)
            return ValidationErrors.TicketsMissing;

        if (AdditionalDetails is { Length: > 0 } &&
            AdditionalDetails.Any(d => string.IsNullOrWhiteSpace(d) || !d.Contains('=')))
        {
            return ValidationResult.Error("Additional details must be specified as key=value pairs.");
        }

        return base.Validate();
    }
}

public class AddRegistrationCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<AddRegistrationSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context, AddRegistrationSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new AdminRegisterAttendeeHttpRequest
        {
            Email = settings.Email!,
            TicketTypeSlugs = settings.TicketTypeSlugs!,
            AdditionalDetails = ParseAdditionalDetails(settings.AdditionalDetails)
        };

        var response = await admittoService.QueryAsync(
            client => client.AdminRegisterAttendeeAsync(teamSlug, eventSlug, request, cancellationToken));

        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage(
            $"Registration added for {settings.Email} (ID: {response.RegistrationId}).");
        return 0;
    }

    private static IDictionary<string, string>? ParseAdditionalDetails(string[]? details)
    {
        if (details is null || details.Length == 0) return null;

        var dict = new Dictionary<string, string>();
        foreach (var entry in details)
        {
            var sep = entry.IndexOf('=');
            var key = entry[..sep].Trim();
            var value = entry[(sep + 1)..].Trim();
            dict[key] = value;
        }
        return dict;
    }
}
