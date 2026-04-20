using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Coupon;

public class RevokeCouponSettings : TeamEventSettings
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

public class RevokeCouponCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<RevokeCouponSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context, RevokeCouponSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var success = await admittoService.SendAsync(
            client => client.RevokeCouponAsync(
                settings.CouponId!.Value, teamSlug, eventSlug, cancellationToken));

        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Coupon revoked.");
        return 0;
    }
}
