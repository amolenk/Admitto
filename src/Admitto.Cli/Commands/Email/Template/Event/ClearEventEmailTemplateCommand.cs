using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Event;

public class ClearEventEmailTemplateSettings : TeamEventSettings
{
    [CommandOption("--type")]
    [Description("The type of email template to clear")]
    public string? EmailType { get; init; }

    public override ValidationResult Validate()
    {
        return EmailType is null ? ValidationErrors.EmailTypeMissing : base.Validate();
    }
}

public class ClearEventEmailTemplateCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<ClearEventEmailTemplateSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ClearEventEmailTemplateSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);
        
        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].EmailTemplates[settings.EmailType]
                .DeleteAsync());
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully cleared event-level template for '{settings.EmailType}' emails.");
        return 0;
    }
}