using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.AttendeeRegistrations.InviteAttendee;

/// <summary>
/// Represents the endpoint for inviting an attendee for a ticketed event.
/// Invited attendees are always accepted, even if the event is full.
/// </summary>
public static class InviteAttendeeEndpoint
{
    public static RouteGroupBuilder MapInviteAttendee(this RouteGroupBuilder group)
    {
        group
            .MapPost("/invite", InviteAttendee)
            .WithName(nameof(InviteAttendee))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok<InviteAttendeeResponse>> InviteAttendee(
        string teamSlug,
        string eventSlug,
        InviteAttendeeRequest attendeeRequest,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var (_, eventId) = 
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents.GetEntityAsync(eventId, cancellationToken: cancellationToken);

        var registrationId = ticketedEvent.InviteAttendee(
            attendeeRequest.Email.NormalizeEmail(),
            attendeeRequest.FirstName,
            attendeeRequest.LastName,
            attendeeRequest.AdditionalDetails.Select(ad => new AdditionalDetail(ad.Name, ad.Value)).ToList(),
            attendeeRequest.Tickets.Select(t => new TicketSelection(t.TicketTypeSlug, t.Quantity)).ToList());
        
        return TypedResults.Ok(new InviteAttendeeResponse(registrationId));
    }
}