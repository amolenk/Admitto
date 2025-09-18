using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.AddTicketType;

/// <summary>
/// Create a new ticket type.
/// </summary>
public static class AddTicketTypeEndpoint
{
    public static RouteGroupBuilder MapAddTicketType(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{eventSlug}/ticket-types", AddTicketType)
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
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents
            .FirstOrDefaultAsync(te => te.Id == eventId, cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        // Split semicolon-separated slot names to support multiple slots
        var slotNames = request.SlotName.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
        
        ticketedEvent.AddTicketType(request.Slug, request.Name, slotNames, request.MaxCapacity);

        return TypedResults.Created();
    }
}