using Amolenk.Admitto.Organization.Application.UseCases.Teams.GetTeam.AdminApi;
using Amolenk.Admitto.Organization.Application.UseCases.Users.AssignTeamMembership.AdminApi;

namespace Amolenk.Admitto.Organization.Application;

public static class OrganizationModule
{
    public const string Key = nameof(Organization);
    
    public static RouteGroupBuilder MapOrganizationAdminEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}")
            .MapAssignTeamMembership()
            .MapGetTeam();
        
        return group;
    }
}