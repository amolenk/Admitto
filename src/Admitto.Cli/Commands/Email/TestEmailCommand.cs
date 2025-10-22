using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Email;

public class TestEmailSettings : TeamEventSettings
{
    [CommandOption("--emailType")]
    [Description("The type of email to test")]
    public string? EmailType { get; init; }

    [CommandOption("--recipient")]
    [Description("The recipient of the test email")]
    public required string Recipient { get; init; }
    
    [CommandOption("--additionalDetail")]
    [Description("Additional details to include in the email in the format 'Name=Value'")]
    public string[]? AdditionalDetails { get; set; } = null!;

    [CommandOption("--ticket")]
    [Description("Ticket(s) to include in the email")]
    public string[]? Tickets { get; set; }
    
    public override ValidationResult Validate()
    {
        if (EmailType is null)
        {
            return ValidationErrors.EmailTypeMissing;
        }

        if (string.IsNullOrWhiteSpace(Recipient))
        {
            return ValidationErrors.EmailRecipientMissing;
        }

        return base.Validate();
    }
}

public class TestEmailCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<TestEmailSettings>
{
    public sealed override async Task<int> ExecuteAsync(CommandContext context, TestEmailSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new TestEmailRequest
        {
            Recipient = settings.Recipient,
            AdditionalDetails = InputHelper.ParseAdditionalDetails(settings.AdditionalDetails),
            Tickets = InputHelper.ParseTickets(settings.Tickets)
        };

        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Emails[settings.EmailType].Test.PostAsync(request));
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully requested '{settings.EmailType}' test mail for '{settings.Recipient}'.");
        return 0;
    }
}
