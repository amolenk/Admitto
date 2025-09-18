namespace Amolenk.Admitto.Cli.Commands.Email.Bulk;

public class ScheduleSettings : TeamEventSettings
{
    [CommandOption("--type")]
    public string? EmailType { get; init; }
    
    [CommandOption("--repeat-start")]
    public DateTimeOffset? RepeatWindowStart { get; init; }

    [CommandOption("--repeat-end")]
    public DateTimeOffset? RepeatWindowEnd { get; init; }

    [CommandOption("--repeat-interval")]
    public TimeSpan? RepeatInterval { get; init; }

    public override ValidationResult Validate()
    {
        if (EmailType is null)
        {
            return ValidationErrors.EmailTypeMissing;
        }

        if (!IsRepeatSet()) return base.Validate();
        
        if (!RepeatWindowStart.HasValue)
        {
            return ValidationErrors.RepeatWindowStartMissing;
        }
            
        if (!RepeatWindowEnd.HasValue)
        {
            return ValidationErrors.RepeatWindowEndMissing;
        }

        if (!RepeatInterval.HasValue)
        {
            return ValidationErrors.RepeatIntervalMissing;
        }

        return base.Validate();
    }
    
    public bool IsRepeatSet()
    {
        return RepeatWindowStart.HasValue || RepeatWindowEnd.HasValue || RepeatInterval.HasValue;
    }
}

public class ScheduleCommand(
    IAccessTokenProvider accessTokenProvider,
    IConfiguration configuration)
    : ApiCommand<ScheduleSettings>(accessTokenProvider, configuration)
{
    public sealed override async Task<int> ExecuteAsync(CommandContext context, ScheduleSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        RepeatDto? repeat = null;
        if (settings.IsRepeatSet())
        {
            repeat = new RepeatDto
            {
                WindowStart = settings.RepeatWindowStart!.Value,
                WindowEnd = settings.RepeatWindowEnd!.Value
            };
        }
        
        var request = new ScheduleBulkEmailRequest
        {
            EmailType = settings.EmailType,
            Repeat = repeat
        };
  
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Emails.Bulk.PostAsync(request));
        if (!response) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully submitted '{settings.EmailType}' email bulk.[/]");
        
        return 0;
    }
}

