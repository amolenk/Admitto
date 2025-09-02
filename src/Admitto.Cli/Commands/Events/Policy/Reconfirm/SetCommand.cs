using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Reconfirm;

public class SetSettings : TeamEventSettings
{
    [CommandOption("--window-start")]
    public TimeSpan? WindowStartBeforeEvent { get; set; }

    [CommandOption("--window-end")]
    public TimeSpan? WindowEndBeforeEvent { get; set; }

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

public class SetCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<SetSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new SetReconfirmPolicyRequest
        {
            WindowStartBeforeEvent = settings.WindowStartBeforeEvent!.Value.ToString(),
            WindowEndBeforeEvent = settings.WindowEndBeforeEvent!.Value.ToString(),
            InitialDelayAfterRegistration = settings.InitialDelayAfterRegistration!.Value.ToString(),
            ReminderInterval = settings.ReminderInterval!.Value.ToString(),
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Policies.Reconfirm.PutAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine($"[green]✓ Successfully set reconfirm policy.[/]");
        return 0;
    }
}