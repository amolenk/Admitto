using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Data;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

/// <summary>
/// Create a new ticketed event.
/// </summary>
public static class CreateTicketedEventEndpoint
{
    public static RouteGroupBuilder MapCreateTicketedEvent(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", CreateTicketedEvent)
            .WithName(nameof(CreateTicketedEvent))
            .RequireAuthorization(policy => policy.RequireCanCreateEvent());
        
        return group;
    }

    private static async ValueTask<Created> CreateTicketedEvent(string teamSlug, CreateTicketedEventRequest request, 
        IDomainContext context, CancellationToken cancellationToken)
    {
        var teamId = await context.Teams.GetTeamIdAsync(teamSlug, cancellationToken);
        
        var newEvent = TicketedEvent.Create(teamId, request.Slug, request.Name, request.StartTime.ToUniversalTime(), 
            request.EndTime.ToUniversalTime(), request.RegistrationStartTime.ToUniversalTime(), 
            request.RegistrationEndTime.ToUniversalTime());

        context.TicketedEvents.Add(newEvent);
        
        return TypedResults.Created($"/teams/{teamSlug}/events/{newEvent.Slug}");
    }
}
