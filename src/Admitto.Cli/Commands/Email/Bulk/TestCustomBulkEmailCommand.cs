// using Amolenk.Admitto.Cli.Common;
//
// namespace Amolenk.Admitto.Cli.Commands.Email.Bulk;
//
// public class TestCustomBulkEmailSettings : TeamEventSettings
// {
//     [CommandOption("--type")]
//     [Description("The type of bulk email to schedule")]
//     public string? EmailType { get; init; }
//     
//     [CommandOption("--recipient")]
//     [Description("The recipient of the test email")]
//     public string? Recipient { get; init; }
//     
//     [CommandOption("--list")]
//     [Description("The email recipient list to use for test input")]
//     public string? ListName { get; init; }
//
//     [CommandOption("--max")]
//     [Description("The maximum number of test emails to send")]
//     public int? MaxCount { get; init; }
//     
//     [CommandOption("--excludeAttendees")]
//     [Description("Whether or not to exclude attendees from the email bulk")]
//     public bool? ExcludeAttendees { get; init; }
//
//     public override ValidationResult Validate()
//     {
//         if (EmailType is null)
//         {
//             return ValidationErrors.EmailTypeMissing;
//         }
//         
//         if (string.IsNullOrWhiteSpace(Recipient))
//         {
//             return ValidationErrors.EmailRecipientMissing;
//         }
//         
//         if (ListName is null)
//         {
//             return ValidationErrors.EmailRecipientListMissing;
//         }
//
//         return base.Validate();
//     }
// }
//
// public class TestCustomBulkEmailCommand(IApiService apiService, IConfigService configService)
//     : AsyncCommand<TestCustomBulkEmailSettings>
// {
//     public sealed override async Task<int> ExecuteAsync(CommandContext context, TestCustomBulkEmailSettings settings, CancellationToken cancellationToken)
//     {
//         var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
//         var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);
//
//         var request = new SendCustomBulkEmailRequest
//         {
//             EmailType = settings.EmailType,
//             RecipientListName = settings.ListName,
//             ExcludeAttendees = settings.ExcludeAttendees,
//             TestOptions = new TestOptionsDto
//             {
//                 Recipient = settings.Recipient,
//                 MaxEmailCount = settings.MaxCount ?? 1
//             }
//         };
//   
//         var response = await apiService.CallApiAsync(async client =>
//             await client.Teams[teamSlug].Events[eventSlug].Emails.Bulk.Custom.PostAsync(request));
//         if (!response) return 1;
//
//         AnsiConsoleExt.WriteSuccesMessage($"Successfully requested {settings.EmailType} test email bulk for {settings.ListName} recipient list.");
//         return 0;
//     }
// }
//
