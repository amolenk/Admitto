using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Data;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketTypes;

// TODO Do we really need this? 
public static class GetTicketTypesEndpoint
{
    public static RouteGroupBuilder MapGetTicketTypes(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{eventSlug}/ticketTypes", GetTicketTypes)
            .WithName(nameof(GetTicketTypes))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());
        
        return group;
    }

    private static async ValueTask<Ok<GetTicketTypesResponse>> GetTicketTypes(string teamSlug, string eventSlug,
        IDomainContext context, CancellationToken cancellationToken)
    {
        var teamId = await context.Teams.GetTeamIdAsync(teamSlug, cancellationToken);
        var ticketedEvent = await context.TicketedEvents.GetTicketedEventAsync(teamId, eventSlug, true,
            cancellationToken);

        var response = new GetTicketTypesResponse(ticketedEvent.TicketTypes
            .Select(t => new TicketTypeDto(t.Name, t.SlotName, t.MaxCapacity))
            .ToArray());

        return TypedResults.Ok(response);
    }
}
