using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.UseCases.Attendees.CancelRegistration;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.Application.UseCases.Public.Cancel;

/// <summary>
/// Represents the endpoint for cancelling an existing registration using the public identifier and a signature.
/// </summary>
public static class CancelEndpoint
{
    public static RouteGroupBuilder MapCancel(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/{publicId:guid}", Cancel)
            .WithName(nameof(Cancel));

        return group;
    }

    private static async ValueTask<Ok> Cancel(
        string teamSlug,
        string eventSlug,
        Guid publicId,
        string signature,
        ISigningService signingService,
        ISlugResolver slugResolver,
        IApplicationContext context,
        [FromServices] CancelRegistrationHandler handler,
        CancellationToken cancellationToken)
    {
        var eventId= await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        if (!await signingService.IsValidAsync(publicId, signature, eventId, cancellationToken))
        {
            throw new ApplicationRuleException(ApplicationRuleError.Signing.InvalidSignature);
        }

        var attendeeId = await context.Participants
            .AsNoTracking()
            .Join(
                context.Attendees,
                p => p.Id,
                a => a.ParticipantId,
                (p, a) => new { Participant = p, Attendee = a })
            .Where(x => x.Participant.TicketedEventId == eventId && x.Participant.PublicId == publicId)
            .Select(x => x.Attendee.Id)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (attendeeId == Guid.Empty)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Participant.NotFound);
        }

        await handler.HandleAsync(new CancelRegistrationCommand(eventId, attendeeId), cancellationToken);

        return TypedResults.Ok();
    }
}