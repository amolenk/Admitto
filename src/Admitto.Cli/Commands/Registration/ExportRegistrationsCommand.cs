// using Amolenk.Admitto.Cli.Commands.Events;
// using Humanizer;
// using Microsoft.Extensions.Configuration;
//
// namespace Amolenk.Admitto.Cli.Commands.Registration;
//
// public class ExportRegistrationsCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
//     : RegistrationCommandBase<RegistrationSettings>(accessTokenProvider, configuration)
// {
//     public override async Task<int> ExecuteAsync(CommandContext context, RegistrationSettings settings)
//     {
//         var teamSlug = GetTeamSlug(settings.TeamSlug);
//         var eventSlug = GetEventSlug(settings.EventSlug);
//         
//         var response = await CallApiAsync(
//             async client => await client.Teams[teamSlug].Events[eventSlug].Registrations.GetAsync());
//         if (response is null) return 1;
//
//         var table = new Table();
//         table.AddColumn("Email");
//         table.AddColumn("Name");
//         table.AddColumn("Status");
//         table.AddColumn("Type");
//
//         foreach (var registration in response.Registrations ?? [])
//         {
//             table.AddRow(
//                 registration.Email!,
//                 $"{registration.FirstName} {registration.LastName}",
//                 FormatStatus(registration.Status!.Value),
//                 registration.Type.Humanize());
//         }
//
//         AnsiConsole.Write(table);
//         return 0;
//     }
// }
