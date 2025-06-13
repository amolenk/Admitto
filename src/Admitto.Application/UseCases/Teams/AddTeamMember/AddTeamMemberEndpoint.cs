// using Amolenk.Admitto.Application.Common;
// using Amolenk.Admitto.Domain.Entities;
//
// namespace Amolenk.Admitto.Application.UseCases.Teams.AddTeamMember;
//
// /// <summary>
// /// Add a new user to an organizing team.
// /// </summary>
// public static class AddTeamMemberEndpoint
// {
//     public static RouteGroupBuilder MapAddTeamMember(this RouteGroupBuilder group)
//     {
//         group.MapPost("/{teamId:guid}/members", AddTeamMember);
//         return group;
//     }
//
//     private static async ValueTask<Results<Created, BadRequest<string>>> AddTeamMember(Guid teamId, 
//         AddTeamMemberRequest request, IDomainContext context, CancellationToken cancellationToken)
//     {
//         var team = await context.Teams.FindAsync([request.OrganizingTeamId], cancellationToken);
//         if (team is null)
//         {
//             return TypedResults.BadRequest(Error.TeamNotFound(teamId));
//         }
//
//         var user = User.Create(request.Email, request.Role);
//         
//         team.AddMember(User.Create(request.Email, request.Role));
//
//         return TypedResults.Created();
//     }
// }
