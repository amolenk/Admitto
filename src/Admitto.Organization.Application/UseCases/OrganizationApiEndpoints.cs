using Amolenk.Admitto.Organization.Application.UseCases.Teams.CreateTeam.AdminApi;
using Amolenk.Admitto.Organization.Application.UseCases.Teams.GetTeam.AdminApi;
using Amolenk.Admitto.Organization.Application.UseCases.Teams.UpdateTeam.AdminApi;
using Amolenk.Admitto.Organization.Application.UseCases.TicketedEvents.CreateTicketedEvent.AdminApi;
using Amolenk.Admitto.Organization.Application.UseCases.Users.AssignTeamMembership.AdminApi;

namespace Amolenk.Admitto.Organization.Application.UseCases;

public static class OrganizationApiEndpoints
{
    public static RouteGroupBuilder MapOrganizationAdminEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapGroup("/teams")
            .MapCreateTeam()
            .MapGroup("/{teamSlug}")
            .MapAssignTeamMembership()
            .MapGetTeam()
            .MapUpdateTeam()
            .MapGroup("/events")
            .MapCreateTicketedEvent()
            .MapGroup("/{eventSlug}");

        return group;
    }
}
