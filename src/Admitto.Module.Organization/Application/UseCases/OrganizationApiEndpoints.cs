using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.ArchiveTeam.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.CreateTeam.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeam.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeams.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.UpdateTeam.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.AddTicketType.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.ArchiveTicketedEvent.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CancelTicketedEvent.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CancelTicketType.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CreateTicketedEvent.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEvent.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEvents.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.UpdateTicketedEvent.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.UpdateTicketType.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers.AdminApi;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.RemoveTeamMembership.AdminApi;

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
            .MapAssignTeamMembership();

        team.MapGroup("/members")
            .MapChangeTeamMembershipRole()
            .MapRemoveTeamMembership();

        var events = team.MapGroup("/events");

        events
            .MapCreateTicketedEvent()
            .MapGetTicketedEvents();

        var eventGroup = events.MapGroup("/{eventSlug}");

        eventGroup
            .MapGetTicketedEvent()
            .MapUpdateTicketedEvent()
            .MapCancelTicketedEvent()
            .MapArchiveTicketedEvent()
            .MapAddTicketType();

        eventGroup.MapGroup("/ticket-types")
            .MapUpdateTicketType()
            .MapCancelTicketType();

        return group;
    }
}
