using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Coupon;

public class ShowCouponSettings : TeamEventSettings
{
    [CommandOption("--id")]
    [Description("The coupon ID")]
    public Guid? CouponId { get; init; }

    public override ValidationResult Validate()
    {
        if (CouponId is null || CouponId == Guid.Empty)
            return ValidationErrors.CouponIdMissing;

        return base.Validate();
    }
}

public class ShowCouponCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<ShowCouponSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context, ShowCouponSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await admittoService.QueryAsync(
            client => client.GetCouponDetailsAsync(
                teamSlug, eventSlug, settings.CouponId!.Value, cancellationToken));

        if (response is null)
        {
            AnsiConsoleExt.WriteErrorMessage("Coupon not found.");
            return 1;
        }

        AnsiConsole.Write(new Rule($"Coupon {response.Id.ToString()[..8]}")
        {
            Justification = Justify.Left,
            Style = Style.Parse("cyan")
        });

        var grid = new Grid();
        grid.AddColumn(new GridColumn { Width = 24 });
        grid.AddColumn();

        grid.AddRow("Email:", response.Email ?? "-");
        grid.AddRow("Code:", response.Code.ToString());
        grid.AddRow("Status:", response.Status?.Humanize() ?? "-");
        grid.AddRow("Expires at:", response.ExpiresAt.Format());
        grid.AddRow("Bypass window:", response.BypassRegistrationWindow ? "Yes" : "No");

        var ticketTypes = response.AllowedTicketTypeSlugs is { Length: > 0 }
            ? string.Join(", ", response.AllowedTicketTypeSlugs)
            : "-";
        grid.AddRow("Ticket types:", ticketTypes);

        grid.AddRow("Created at:", response.CreatedAt.Format());

        if (response.RedeemedAt.HasValue)
            grid.AddRow("Redeemed at:", response.RedeemedAt.Value.Format());

        if (response.RevokedAt.HasValue)
            grid.AddRow("Revoked at:", response.RevokedAt.Value.Format());

        AnsiConsole.Write(grid);
        return 0;
    }
}
