using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.ArchiveTeam.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.CreateTeam.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeam.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeams.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.UpdateTeam.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.RemoveTeamMembership.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.GetEventCreationRequest.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation.AdminApi;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases;

public static class OrganizationApiEndpoints
{
    public static RouteGroupBuilder MapOrganizationAdminEndpoints(this RouteGroupBuilder group)
    {
        var teams = group.MapGroup("/teams");

        teams
            .MapCreateTeam()
            .MapGetTeams();

        var team = teams.MapGroup("/{teamSlug}");

        team
            .MapGetTeam()
            .MapUpdateTeam()
            .MapArchiveTeam()
            .MapListTeamMembers()
            .MapAssignTeamMembership()
            .MapRequestTicketedEventCreation()
            .MapGetEventCreationRequest();

        team.MapGroup("/members")
            .MapChangeTeamMembershipRole()
            .MapRemoveTeamMembership();

        return group;
    }
}
