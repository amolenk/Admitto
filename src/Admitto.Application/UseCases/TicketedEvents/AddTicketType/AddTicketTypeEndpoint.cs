using Amolenk.Admitto.Application.Common.Authorization;

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

    private static async ValueTask<Created> AddTicketType(
        string teamSlug,
        string eventSlug,
        AddTicketTypeRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var (_, eventId) =
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents.GetEntityAsync(eventId, cancellationToken: cancellationToken);
        
        ticketedEvent.AddTicketType(request.Slug, request.Name, request.SlotName, request.MaxCapacity);

        return TypedResults.Created();
    }
}