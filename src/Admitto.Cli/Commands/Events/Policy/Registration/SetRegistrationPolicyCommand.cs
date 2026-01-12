using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Registration;

public class SetSettings : TeamEventSettings
{
    [CommandOption("--opens-before")]
    [Description("The timespan before the event when registration opens")]
    public TimeSpan? OpensBeforeEvent { get; set; }

    [CommandOption("--closes-before")]
    [Description("The timespan before the event when registration closes")]
    public TimeSpan? ClosesBeforeEvent { get; set; }

    public override ValidationResult Validate()
    {
        if (OpensBeforeEvent is null)
        {
            return ValidationErrors.OpensBeforeEventMissing;
        }

        if (ClosesBeforeEvent is null)
        {
            return ValidationErrors.ClosesBeforeEventMissing;
        }

        return base.Validate();
    }
}

public class SetRegistrationPolicyCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<SetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SetSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new SetRegistrationPolicyRequest
        {
            OpensBeforeEvent = settings.OpensBeforeEvent!.Value,
            ClosesBeforeEvent = settings.ClosesBeforeEvent!.Value
        };

        var result = await admittoService.SendAsync(client =>
            client.SetRegistrationPolicyAsync(teamSlug, eventSlug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully set registration policy.");
        return 0;
    }
}