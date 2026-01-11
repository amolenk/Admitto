using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

/// <summary>
/// Represents the endpoint for creating a new registration on behalf of an attendee.
/// These types of registrations are typically created by event organizers or administrators and ignore ticket limits.
/// </summary>
public static class RegisterAttendeeEndpoint
{
    public static RouteGroupBuilder MapRegisterAttendee(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", RegisterAttendee)
            .WithName(nameof(RegisterAttendee))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Organizer));

        return group;
    }

    private static async ValueTask<Created> RegisterAttendee(
        [FromRoute] string teamSlug,
        [FromRoute] string eventSlug,
        [FromBody] RegisterAttendeeRequest request,
        [FromServices] ISlugResolver slugResolver,
        [FromServices] RegisterAttendeeHandler registerAttendeeHandler,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        var command = new RegisterAttendeeCommand(
            teamId,
            eventId,
            request.Email.NormalizeEmail(),
            request.FirstName,
            request.LastName,
            request.AdditionalDetails.Select(ad => new AdditionalDetail(ad.Name, ad.Value)).ToList(),
            request.AssignedTickets.Select(t => new TicketSelection(t.TicketTypeSlug, t.Quantity)).ToList(),
            Coupons: [],
            AdminOnBehalfOf: true);

        await registerAttendeeHandler.HandleAsync(command, cancellationToken);
        
        return TypedResults.Created();
    }
}