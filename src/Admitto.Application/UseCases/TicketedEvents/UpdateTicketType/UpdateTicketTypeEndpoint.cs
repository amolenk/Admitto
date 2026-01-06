using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.UpdateTicketType;

/// <summary> 
/// Updates an existing ticket type.
/// </summary>
public static class UpdateTicketTypeEndpoint
{
    public static RouteGroupBuilder MapUpdateTicketType(this RouteGroupBuilder group)
    {
        group
            .MapPatch("/{eventSlug}/ticket-types/{slug}", UpdateTicketType)
            .WithName(nameof(UpdateTicketType))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> UpdateTicketType(
        string teamSlug,
        string eventSlug,
        string slug,
        UpdateTicketTypeRequest request,
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

        ticketedEvent.UpdateMaxCapacity(slug, request.MaxCapacity);

        return TypedResults.Ok();
    }
}