using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

/// <summary>
/// Get a specific ticketed event (including availability and ticket types).
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
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(te => te.Id == eventId, cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        var ticketTypes = ticketedEvent.TicketTypes
            .Select(tt => new TicketTypeDto(
                tt.Slug,
                tt.Name,
                string.Join(";", tt.SlotNames),
                tt.MaxCapacity,
                tt.UsedCapacity))
            .ToList();

        var additionalDetailSchemas = ticketedEvent.AdditionalDetailSchemas
            .Select(ads => new AdditionalDetailSchemaDto(
                ads.Name,
                ads.MaxLength.ToString(),
                ads.IsRequired))
            .ToList();

        var response = new GetTicketedEventResponse(
            ticketedEvent.Slug,
            ticketedEvent.Name,
            ticketedEvent.StartsAt,
            ticketedEvent.EndsAt,
            ticketedEvent.RegistrationOpensAt,
            ticketedEvent.RegistrationClosesAt,
            ticketedEvent.BaseUrl,
            ticketTypes,
            additionalDetailSchemas);

        return TypedResults.Ok(response);
    }
}