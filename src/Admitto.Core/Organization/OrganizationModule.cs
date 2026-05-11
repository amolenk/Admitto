using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.ArchiveTeam.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.CreateTeam.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.GetTeam.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.GetTeams.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.UpdateTeam.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.RemoveTeamMembership.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.GetEventCreationRequest.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.CreateApiKey.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.GetApiKeys.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.RevokeApiKey.AdminApi;

namespace Amolenk.Admitto.Core.Organization;

public static class OrganizationModule
{
    public const string Key = nameof(Organization);

    public static RouteGroupBuilder MapOrganizationAdminEndpoints(this RouteGroupBuilder group)
    {
        var teams = group.MapGroup("/teams");

        teams
            .MapCreateTeam()
            .MapGetTeams();

        var team = teams.MapGroup("/{teamId:guid}");

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

        team.MapGroup("/api-keys")
            .MapCreateApiKey()
            .MapGetApiKeys()
            .MapRevokeApiKey();

        return group;
    }
}