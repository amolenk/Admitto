using Amolenk.Admitto.Application.Common.Authorization;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

/// <summary>
/// Get a specific ticketed event.
/// </summary>
public static class GetTicketedEventEndpoint
{
    public static RouteGroupBuilder MapGetTicketedEvent(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{eventSlug}", GetTicketedEvent)
            .WithName(nameof(GetTicketedEvent))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<GetTicketedEventResponse>> GetTicketedEvent(
        string teamSlug,
        string eventSlug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var (_, eventId) = 
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents.GetEntityAsync(eventId, true, cancellationToken);

        var ticketTypes = ticketedEvent.TicketTypes.Select(t => new TicketTypeDto(
            t.Slug,
            t.Name,
            t.SlotName,
            t.MaxCapacity,
            t.UsedCapacity));

        var response = new GetTicketedEventResponse(
            ticketedEvent.Slug,
            ticketedEvent.Name,
            ticketedEvent.StartTime,
            ticketedEvent.EndTime,
            ticketedEvent.RegistrationStartTime,
            ticketedEvent.RegistrationEndTime,
            ticketTypes);

        return TypedResults.Ok(response);
    }
}