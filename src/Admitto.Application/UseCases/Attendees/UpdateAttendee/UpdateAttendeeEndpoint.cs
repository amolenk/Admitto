using Amolenk.Admitto.Application.UseCases.Attendees.ChangeTickets;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.UpdateAttendee;

/// <summary>
/// Represents the endpoint for updating an existing attendee.
/// </summary>
public static class UpdateAttendeeEndpoint
{
    public static RouteGroupBuilder MapUpdateAttendee(this RouteGroupBuilder group)
    {
        group
            .MapPut("/{attendeeId}", UpdateAttendee)
            .WithName(nameof(UpdateAttendee))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> UpdateAttendee(
        [FromRoute] string teamSlug,
        [FromRoute] string eventSlug,
        [FromRoute] Guid attendeeId,
        [FromBody] UpdateAttendeeRequest request,
        [FromServices] ISlugResolver slugResolver,
        [FromServices] ChangeTicketsHandler changeTicketsHandler,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var command = new ChangeTicketsCommand(
            eventId,
            attendeeId,
            request.Tickets.Select(t => new TicketSelection(t.TicketTypeSlug, t.Quantity)).ToList(),
            AdminOnBehalfOf: true);

        await changeTicketsHandler.HandleAsync(command, cancellationToken);

        return TypedResults.Ok();
    }
}