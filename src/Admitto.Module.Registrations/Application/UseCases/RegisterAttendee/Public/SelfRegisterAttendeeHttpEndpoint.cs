// using System.Security.Claims;
// using Amolenk.Admitto.Module.Organization.Contracts;
// using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
// using Amolenk.Admitto.Module.Shared.Application.Messaging;
// using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
// using Microsoft.AspNetCore.Http.HttpResults;
//
// namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegisterAttendee.Public;
//
// /// <summary>
// /// Represents the endpoint for creating a new registration on behalf of an attendee.
// /// These types of registrations are typically created by event organizers or administrators and ignore ticket limits.
// /// </summary>
// public static class SelfRegisterAttendeeHttpEndpoint
// {
//     public static RouteGroupBuilder MapRegisterAttendee(this RouteGroupBuilder group)
//     {
//         group
//             .MapPost("/", RegisterAttendee)
//             .WithName(nameof(RegisterAttendee));
//             // TODO
//             // .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Organizer));
//
//         return group;
//     }
//
//     private static async ValueTask<Created<SelfRegisterAttendeeHttpResponse>> RegisterAttendee(
//         string teamSlug,
//         string eventSlug,
//         SelfRegisterAttendeeHttpRequest httpRequest,
//         ClaimsPrincipal user,
//         IOrganizationFacade organizationFacade,
//         IMediator mediator,
//         CancellationToken cancellationToken)
//     {
//         throw new NotImplementedException();
//     }
// }