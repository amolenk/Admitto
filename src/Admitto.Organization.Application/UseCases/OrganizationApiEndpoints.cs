using Amolenk.Admitto.Organization.Application.UseCases.Teams.CreateTeam.AdminApi;
using Amolenk.Admitto.Organization.Application.UseCases.Teams.GetTeam.AdminApi;
using Amolenk.Admitto.Organization.Application.UseCases.Teams.UpdateTeam.AdminApi;
using Amolenk.Admitto.Organization.Application.UseCases.Users.AssignTeamMembership.AdminApi;

namespace Amolenk.Admitto.Organization.Application;

public static class OrganizationApiEndpoints
{
    public static RouteGroupBuilder MapOrganizationAdminEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapGroup("/teams")
            .MapCreateTeam()
            .MapGroup("/{teamSlug}")
            .MapUpdateTeam()
            .MapGroup("/events")
            .MapGroup("/{eventSlug}")
            .MapAssignTeamMembership()
            .MapGetTeam();

        return group;
    }
}