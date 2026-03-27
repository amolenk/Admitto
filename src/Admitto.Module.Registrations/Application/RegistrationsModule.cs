// using Amolenk.Admitto.Module.Registrations.Application.UseCases.RegisterAttendee.Admin;
//
// namespace Amolenk.Admitto.Module.Registrations.Application;
//
// public static class RegistrationsModule
// {
//     public const string Key = nameof(Registrations);
//     
//     public static RouteGroupBuilder MapRegistrationsAdminEndpoints(this RouteGroupBuilder group)
//     {
//         group
//             .MapGroup("/teams/{teamSlug}/events/{eventSlug}")
//             .MapRegisterAttendee();
//         
//         return group;
//     }
// }