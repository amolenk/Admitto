using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Reconfirm;

public class SetSettings : TeamEventSettings
{
    [CommandOption("--window-start")] public TimeSpan? WindowStartBeforeEvent { get; set; }

    [CommandOption("--window-end")] public TimeSpan? WindowEndBeforeEvent { get; set; }

    [CommandOption("--initial-delay")]
    [Description("Initial delay after registration before the first reconfirmation email is sent.")]
    public TimeSpan? InitialDelayAfterRegistration { get; set; }

    [CommandOption("--reminder-interval")]
    [Description("Interval between reconfirmation reminder emails. Set to 0 to disable reminders.")]
    public TimeSpan? ReminderInterval { get; set; }

    public override ValidationResult Validate()
    {
        if (WindowStartBeforeEvent is null)
        {
            return ValidationErrors.ReconfirmWindowStartMissing;
        }

        if (WindowEndBeforeEvent is null)
        {
            return ValidationErrors.ReconfirmWindowEndMissing;
        }

        if (InitialDelayAfterRegistration is null)
        {
            return ValidationErrors.ReconfirmInitialDelayMissing;
        }

        if (ReminderInterval is null)
        {
            return ValidationErrors.ReconfirmReminderIntervalDelayMissing;
        }

        return base.Validate();
    }
}

public class SetReconfirmPolicyCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<SetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SetSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new SetReconfirmPolicyRequest
        {
            WindowStartBeforeEvent = settings.WindowStartBeforeEvent!.Value,
            WindowEndBeforeEvent = settings.WindowEndBeforeEvent!.Value,
            InitialDelayAfterRegistration = settings.InitialDelayAfterRegistration!.Value,
            ReminderInterval = settings.ReminderInterval!.Value,
        };

        var result = await admittoService.SendAsync(client =>
            client.SetReconfirmPolicyAsync(teamSlug, eventSlug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully set reconfirm policy.");
        return 0;
    }
}