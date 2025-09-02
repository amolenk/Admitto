using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.UseCases.Attendees.ReconfirmRegistration;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.Application.UseCases.Participants.ReconfirmParticipation;

/// <summary>
/// Represents the endpoint for reconfirming an existing registration using the public identifier and a signature.
/// </summary>
public static class ReconfirmParticipationEndpoint
{
    public static RouteGroupBuilder MapReconfirmParticipation(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{publicId:guid}/reconfirm", ReconfirmParticipation)
            .WithName(nameof(ReconfirmParticipation))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> ReconfirmParticipation(
        string teamSlug,
        string eventSlug,
        Guid publicId,
        string signature,
        ISigningService signingService,
        ISlugResolver slugResolver,
        IApplicationContext context,
        [FromServices] ReconfirmRegistrationHandler handler,
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

        await handler.HandleAsync(new ReconfirmRegistrationCommand(eventId, attendeeId), cancellationToken);

        return TypedResults.Ok();
    }
}