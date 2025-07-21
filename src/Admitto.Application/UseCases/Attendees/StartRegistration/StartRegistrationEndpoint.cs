using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Validation;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Attendees.StartRegistration;

public static class StartRegistrationEndpoint
{
    public static RouteGroupBuilder MapStartRegistration(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", StartRegistration)
            .WithName(nameof(StartRegistration))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Accepted<StartRegistrationResponse>> StartRegistration(
        string teamSlug,
        string eventSlug,
        StartRegistrationRequest request,
        IDomainContext context,
        CancellationToken cancellationToken)
    {
        var teamId = await context.Teams.GetTeamIdAsync(teamSlug, cancellationToken);
        var ticketedEvent = await context.TicketedEvents.GetTicketedEventAsync(
            teamId,
            eventSlug,
            noTracking: true,
            cancellationToken);

        // If there's not enough capacity, reject immediately.
        // Invited attendees always get a spot, so we don't check capacity for them.
        if (!request.IsInvited && !ticketedEvent.HasAvailableCapacity(request.Tickets))
        {
            throw ValidationError.TicketedEvent.SoldOut();
        }
        
        // TODO Check that there isn't already a registration request for this event with the same email.
        
        var attendee = Attendee.Create(
            teamId,
            ticketedEvent.Id,
            request.Email,
            request.FirstName,
            request.LastName,
            request.AdditionalDetails,
            request.Tickets,
            request.IsInvited);

        context.Attendees.Add(attendee);

        return TypedResults.Accepted(
            $"/teams/{teamSlug}/events/{eventSlug}/attendees/{attendee.Id}",
            new StartRegistrationResponse(attendee.Id));
    }
}