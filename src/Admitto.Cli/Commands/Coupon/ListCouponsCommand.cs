using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Coupon;

public class ListCouponsCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context, TeamEventSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await admittoService.QueryAsync(
            client => client.ListCouponsAsync(teamSlug, eventSlug, cancellationToken));

        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Email");
        table.AddColumn("Status");
        table.AddColumn("Ticket Types");
        table.AddColumn("Expires At");
        table.AddColumn("Created At");

        foreach (var coupon in response.Coupons ?? [])
        {
            var ticketTypes = coupon.AllowedTicketTypeSlugs is { Length: > 0 }
                ? string.Join(", ", coupon.AllowedTicketTypeSlugs)
                : "-";

            table.AddRow(
                coupon.Id.ToString()[..8],
                coupon.Email ?? "-",
                coupon.Status?.Humanize() ?? "-",
                ticketTypes,
                coupon.ExpiresAt.Format(),
                coupon.CreatedAt.Format());
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
