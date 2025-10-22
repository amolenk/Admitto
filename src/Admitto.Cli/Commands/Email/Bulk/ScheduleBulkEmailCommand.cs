using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Email.Bulk;

public class ScheduleBulkEmailSettings : TeamEventSettings
{
    [CommandOption("--type")]
    [Description("The type of bulk email to schedule")]
    public string? EmailType { get; init; }
    
    [CommandOption("--repeat-start")]
    [Description("The start of the repeat window (optional)")]
    public DateTimeOffset? RepeatWindowStart { get; init; }

    [CommandOption("--repeat-end")]
    [Description("The end of the repeat window (optional)")]
    public DateTimeOffset? RepeatWindowEnd { get; init; }

    [CommandOption("--repeat-interval")]
    [Description("The repeat interval (optional)")]
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

public class ScheduleBulkEmailCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<ScheduleBulkEmailSettings>
{
    public sealed override async Task<int> ExecuteAsync(CommandContext context, ScheduleBulkEmailSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

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
  
        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Emails.Bulk.PostAsync(request));
        if (!response) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully submitted '{settings.EmailType}' email bulk.");
        
        return 0;
    }
}

