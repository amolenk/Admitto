using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Registrations.Register;

/// <summary>
/// Represents the endpoint for creating a new registration for a ticketed event.
/// </summary>
public static class RegisterEndpoint
{
    public static RouteGroupBuilder MapRegister(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", Register)
            .WithName(nameof(Register))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok<RegisterResponse>> Register(
        string teamSlug,
        string eventSlug,
        RegisterRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var (_, eventId) = 
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents.GetEntityAsync(eventId, cancellationToken: cancellationToken);

        var registrationId = ticketedEvent.Register(
            request.Email,
            request.FirstName,
            request.LastName,
            request.AdditionalDetails.Select(ad => new AdditionalDetail(ad.Name, ad.Value)).ToList(),
            request.Tickets.Select(t => new TicketSelection(t.TicketTypeSlug, t.Quantity)).ToList(),
            request.IsInvited);
        
        return TypedResults.Ok(new RegisterResponse(registrationId));
    }
}