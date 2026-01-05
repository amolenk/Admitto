using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Application.UseCases.Attendees.ChangeTickets;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Public.ChangeTickets;

/// <summary>
/// Changes the tickets of an existing registration.
/// </summary>
public static class ChangeTicketsEndpoint
{
    public static RouteGroupBuilder MapChangeTickets(this RouteGroupBuilder group)
    {
        group
            .MapPut("/{publicId:guid}/tickets", ChangeTickets)
            .WithName(nameof(ChangeTickets));

        return group;
    }
    
    private static async ValueTask<Ok> ChangeTickets(
        [FromRoute] string teamSlug,
        [FromRoute] string eventSlug,
        [FromRoute] Guid publicId,
        [FromQuery] string signature,
        [FromBody] ChangeTicketsRequest request,
        [FromServices] ChangeTicketsHandler changeTicketsHandler,
        [FromServices] IApplicationContext context,
        [FromServices] ISigningService signingService,
        [FromServices] ISlugResolver slugResolver,
        CancellationToken cancellationToken)
    {
        var eventId= await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        if (!await signingService.IsValidAsync(publicId, signature, eventId, cancellationToken))
        {
            throw new ApplicationRuleException(ApplicationRuleError.Signing.InvalidSignature);
        }
        
        var attendeeId = await context.ParticipationView
            .Where(p => p.TicketedEventId == eventId && p.PublicId == publicId)
            .Select(p => p.AttendeeId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (attendeeId is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }
        
        var command = new ChangeTicketsCommand(
            eventId,
            attendeeId.Value,
            request.RequestedTickets.Select(t => new TicketSelection(t, 1)).ToList());

        await changeTicketsHandler.HandleAsync(command, cancellationToken);
        
        return TypedResults.Ok();
    }
}