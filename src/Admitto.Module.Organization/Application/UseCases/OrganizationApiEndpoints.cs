using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.ArchiveTeam.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.CreateTeam.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeam.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeams.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.UpdateTeam.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CreateTicketedEvent.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.Users.AssignTeamMembership.AdminApi;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases;

public static class OrganizationApiEndpoints
{
    public static RouteGroupBuilder MapOrganizationAdminEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapGroup("/teams")
            .MapCreateTeam()
            .MapGetTeams()
            .MapGroup("/{teamSlug}")
            .MapAssignTeamMembership()
            .MapGetTeam()
            .MapUpdateTeam()
            .MapArchiveTeam()
            .MapGroup("/events")
            .MapCreateTicketedEvent()
            .MapGroup("/{eventSlug}");

        return group;
    }
}
