// using Microsoft.Extensions.Configuration;
//
// namespace Amolenk.Admitto.Cli.Commands.Email.Send;
//
// public abstract class SendEmailCommandBase<TSettings>(
//     EmailType emailType,
//     IAccessTokenProvider accessTokenProvider,
//     IConfiguration configuration)
//     : ApiCommand<TSettings>(accessTokenProvider, configuration)
//     where TSettings : TeamEventSettings
// {
//     public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
//     {
//         var teamSlug = GetTeamSlug(settings.TeamSlug);
//         var eventSlug = GetEventSlug(settings.EventSlug);
//
//         var info = await GetEntityInfoAsync(settings);
//         
//         var request = new SendEmailRequest
//         {
//             EmailType = emailType,
//             DataEntityId = info.DataEntityId
//         };
//         
//         // TODO Ask the user for confirmation before sending the email
//
//         var succes = await CallApiAsync(async client =>
//             await client.Teams[teamSlug].Events[eventSlug].Email.PostAsync(request));
//         if (!succes) return 1;
//         
//         AnsiConsole.MarkupLine($"[green]✓ Successfully enqueued email for {info.Email}.[/]");
//         return 0;
//     }
//     
//     protected abstract ValueTask<(Guid DataEntityId, string Email)> GetEntityInfoAsync(TSettings settings);
// }