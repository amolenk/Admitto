using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Coupon;

public class CreateCouponSettings : TeamEventSettings
{
    [CommandOption("--email")]
    [Description("The email address of the coupon recipient")]
    public string? Email { get; init; }

    [CommandOption("--ticket-type")]
    [Description("Allowed ticket type ID (can be specified multiple times)")]
    public Guid[]? TicketTypeIds { get; init; }

    [CommandOption("--expires-at")]
    [Description("Coupon expiry date and time (ISO 8601)")]
    public string? ExpiresAt { get; init; }

    [CommandOption("--bypass-window")]
    [Description("Allow registration outside the normal registration window")]
    public bool BypassRegistrationWindow { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return ValidationErrors.EmailMissing;

        if (TicketTypeIds is null || TicketTypeIds.Length == 0)
            return ValidationErrors.CouponTicketTypesMissing;

        if (string.IsNullOrWhiteSpace(ExpiresAt) || !DateTimeOffset.TryParse(ExpiresAt, out _))
            return ValidationErrors.CouponExpiresAtMissing;

        return base.Validate();
    }
}

public class CreateCouponCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<CreateCouponSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context, CreateCouponSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new CreateCouponRequest
        {
            Email = settings.Email,
            AllowedTicketTypeIds = settings.TicketTypeIds,
            ExpiresAt = DateTimeOffset.Parse(settings.ExpiresAt!),
            BypassRegistrationWindow = settings.BypassRegistrationWindow
        };

        var response = await admittoService.QueryAsync(
            client => client.CreateCouponAsync(teamSlug, eventSlug, request, cancellationToken));

        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage(
            $"Coupon created for {settings.Email} (ID: {response.CouponId}).");
        return 0;
    }
}
