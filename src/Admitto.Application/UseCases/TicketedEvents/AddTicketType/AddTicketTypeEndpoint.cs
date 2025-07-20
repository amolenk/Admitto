using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Data;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.AddTicketType;

/// <summary>
/// Create a new ticket type.
/// </summary>
public static class AddTicketTypeEndpoint
{
    public static RouteGroupBuilder MapAddTicketType(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{eventSlug}/ticketTypes", AddTicketType)
            .WithName(nameof(AddTicketType))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());
        
        return group;
    }

    private static async ValueTask<Created> AddTicketType(string teamSlug, string eventSlug,
        AddTicketTypeRequest request, IDomainContext context, CancellationToken cancellationToken)
    {
        var teamId = await context.Teams.GetTeamIdAsync(teamSlug, cancellationToken);
        var ticketedEvent = await context.TicketedEvents.GetTicketedEventAsync(teamId, eventSlug,
            cancellationToken: cancellationToken);

        ticketedEvent.AddTicketType(request.Slug, request.Name, request.SlotName, request.MaxCapacity);

        return TypedResults.Created();
    }
}
