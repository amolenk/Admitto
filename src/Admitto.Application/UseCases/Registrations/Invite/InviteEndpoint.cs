using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Registrations.Invite;

/// <summary>
/// Represents the endpoint for inviting an attendee for a ticketed event.
/// Invited attendees are always accepted, even if the event is full.
/// </summary>
public static class InviteEndpoint
{
    public static RouteGroupBuilder MapInvite(this RouteGroupBuilder group)
    {
        group
            .MapPost("/invite", Invite)
            .WithName(nameof(Invite))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok<InviteResponse>> Invite(
        string teamSlug,
        string eventSlug,
        InviteRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var (_, eventId) = 
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents.GetEntityAsync(eventId, cancellationToken: cancellationToken);

        var registrationId = ticketedEvent.Invite(
            request.Email.NormalizeEmail(),
            request.FirstName,
            request.LastName,
            request.AdditionalDetails.Select(ad => new AdditionalDetail(ad.Name, ad.Value)).ToList(),
            request.Tickets.Select(t => new TicketSelection(t.TicketTypeSlug, t.Quantity)).ToList());
        
        return TypedResults.Ok(new InviteResponse(registrationId));
    }
}