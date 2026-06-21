using Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.ArchiveTeam.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.CreateTeam.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.GetTeam.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.GetTeams.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.UpdateTeam.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.AssignTeamMembership.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.ChangeTeamMembershipRole.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.GetTeamMembers.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.RemoveTeamMembership.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.GetEventCreationRequest.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RequestTicketedEventCreation.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.CreateApiKey.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.GetApiKeys.AdminApi;
using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.RevokeApiKey.AdminApi;

namespace Amolenk.Admitto.Core.Organization;

public static class OrganizationModule
{
    public const string Key = nameof(Organization);
    public const string NamespacePrefix = "Amolenk.Admitto.Core.Organization";

    public static RouteGroupBuilder MapOrganizationAdminEndpoints(this RouteGroupBuilder group)
    {
        var teams = group.MapGroup("/teams")
            .WithTags("Admin - Teams");

        teams
            .MapCreateTeam()
            .MapGetTeams();

        var team = teams.MapGroup("/{teamId:guid}");

        team
            .MapGetTeam()
            .MapUpdateTeam()
            .MapArchiveTeam()
            .MapGetTeamMembers()
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
