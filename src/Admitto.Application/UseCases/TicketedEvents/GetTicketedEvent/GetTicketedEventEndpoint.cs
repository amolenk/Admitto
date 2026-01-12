using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

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
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Crew));

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
                tt.SlotNames,
                tt.MaxCapacity,
                tt.UsedCapacity))
            .ToList();

        var additionalDetailSchemas = ticketedEvent.AdditionalDetailSchemas
            .Select(ads => new AdditionalDetailSchemaDto(
                ads.Name,
                ads.MaxLength.ToString(),
                ads.IsRequired))
            .ToList();
        
        var reconfirmPolicy = ticketedEvent.ReconfirmPolicy is null ? null : new ReconfirmPolicyDto(
            ticketedEvent.StartsAt - ticketedEvent.ReconfirmPolicy.WindowStartBeforeEvent,
            ticketedEvent.StartsAt - ticketedEvent.ReconfirmPolicy.WindowEndBeforeEvent,
            ticketedEvent.ReconfirmPolicy.InitialDelayAfterRegistration,
            ticketedEvent.ReconfirmPolicy.ReminderInterval);

        var response = new GetTicketedEventResponse(
            ticketedEvent.Slug,
            ticketedEvent.Name,
            ticketedEvent.StartsAt,
            ticketedEvent.EndsAt,
            ticketedEvent.RegistrationOpensAt,
            ticketedEvent.RegistrationClosesAt,
            ticketedEvent.BaseUrl,
            ticketTypes,
            additionalDetailSchemas,
            reconfirmPolicy);

        return TypedResults.Ok(response);
    }
}