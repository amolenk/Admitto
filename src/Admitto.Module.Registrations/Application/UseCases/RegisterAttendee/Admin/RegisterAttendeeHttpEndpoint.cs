// using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
// using Amolenk.Admitto.Module.Shared.Application;
// using Amolenk.Admitto.Module.Shared.Application.Auth;
// using Amolenk.Admitto.Module.Shared.Application.Http;
// using Amolenk.Admitto.Module.Shared.Application.Persistence;
// using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
// using Microsoft.AspNetCore.Http.HttpResults;
//
// namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegisterAttendee.Admin;
//
// /// <summary>
// /// Represents the endpoint for creating a new registration on behalf of an attendee.
// /// These types of registrations are typically created by event organizers or administrators and ignore ticket limits.
// /// </summary>
// public static class RegisterAttendeeHttpEndpoint
// {
//     public static RouteGroupBuilder MapRegisterAttendee(this RouteGroupBuilder group)
//     {
//         group
//             .MapPost("/registrations", RegisterAttendee)
//             .WithName(nameof(RegisterAttendee))
//             .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));
//
//         return group;
//     }
//
//     private static async ValueTask<Created<RegisterAttendeeHttpResponse>> RegisterAttendee(
//         OrganizationScope organizationScope,
//         RegisterAttendeeHttpRequest httpRequest,
//         [FromKeyedServices(RegistrationsModule.Key)]
//         IUnitOfWork unitOfWork,
//         CancellationToken cancellationToken)
//     {
//         var registrationId = await unitOfWork.RunAsync(
//             (mediator, ct) =>
//             {
//                 var command = httpRequest.ToCommand(organizationScope.EventId!.Value);
//                 
//                 return mediator.SendReceiveAsync<RegisterAttendeeCommand, RegistrationId>(command, ct);
//             },
//             cancellationToken);
//
//         return TypedResults.Created(
//             $"/teams/{organizationScope.TeamSlug}/events/{organizationScope.EventSlug}/registrations/{registrationId}",
//             new RegisterAttendeeHttpResponse(registrationId.Value));
//     }
// }