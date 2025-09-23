using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;

namespace Amolenk.Admitto.Application.UseCases.Public.GetTickets;

/// <summary>
/// Gets the tickets of an attendee for a specific ticketed event.
/// </summary>
public static class GetTicketsEndpoint
{
    public static RouteGroupBuilder MapGetTickets(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{publicId:guid}/tickets", GetTickets)
            .WithName(nameof(GetTickets));

        return group;
    }
    
    private static async ValueTask<Results<Ok<GetTicketsResponse>, NotFound>> GetTickets(
        string teamSlug,
        string eventSlug,
        Guid publicId,
        string signature,
        ISigningService signingService,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId= await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        if (!await signingService.IsValidAsync(publicId, signature, eventId, cancellationToken))
        {
            throw new ApplicationRuleException(ApplicationRuleError.Signing.InvalidSignature);
        }
        
        var tickets = await context.Participants
            .AsNoTracking()
            .Join(
                context.Attendees,
                p => p.Id,
                a => a.ParticipantId,
                (p, a) => new { Participant = p, Attendee = a })
            .Where(x => x.Participant.TicketedEventId == eventId && x.Participant.PublicId == publicId)
            .Select(x => x.Attendee.Tickets)
            .FirstOrDefaultAsync(cancellationToken);

        if (tickets is null)
        {
            return TypedResults.NotFound();
        }

        var response = new GetTicketsResponse(tickets.Select(t => t.TicketTypeSlug).ToList());
        
        return TypedResults.Ok(response);
    }
}