using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.Public.GetAvailability;

/// <summary>
/// Gets the ticket availability of an event
/// </summary>
public static class GetAvailabilityEndpoint
{
    public static RouteGroupBuilder MapGetAvailability(this RouteGroupBuilder group)
    {
        group
            .MapGet("/availability", GetAvailability)
            .WithName(nameof(GetAvailability));

        return group;
    }

    private static async ValueTask<Ok<GetAvailabilityResponse>> GetAvailability(
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
                tt.SlotNames,
                tt.UsedCapacity < tt.MaxCapacity))
            .ToList();

        var additionalDetailSchemas = ticketedEvent.AdditionalDetailSchemas
            .Select(ads => new AdditionalDetailSchemaDto(
                ads.Name,
                ads.MaxLength,
                ads.IsRequired))
            .ToList();

        var response = new GetAvailabilityResponse(
            ticketedEvent.RegistrationOpensAt,
            ticketedEvent.RegistrationClosesAt,
            ticketTypes,
            additionalDetailSchemas);

        return TypedResults.Ok(response);
    }
}