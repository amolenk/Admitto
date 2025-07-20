using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Validation;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.PendingRegistrations.StartRegistration;

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
        StartRegistrationRequest startRegistrationRequest,
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
        if (!ticketedEvent.HasAvailableCapacity(startRegistrationRequest.Tickets))
        {
            throw ValidationError.TicketedEvent.SoldOut();
        }
        
        // TODO Check that there isn't already a registration request for this event with the same email.
        
        var registration = PendingRegistration.Create(
            teamId,
            ticketedEvent.Id,
            startRegistrationRequest.Email,
            startRegistrationRequest.FirstName,
            startRegistrationRequest.LastName,
            startRegistrationRequest.AdditionalDetails,
            startRegistrationRequest.Tickets);

        context.PendingRegistrations.Add(registration);

        return TypedResults.Accepted(
            $"/teams/{teamSlug}/events/{eventSlug}/registrations/{registration.Id}",
            new StartRegistrationResponse(registration.Id));
    }
}