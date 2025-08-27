using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Cryptography;

namespace Amolenk.Admitto.Application.UseCases.AttendeeRegistrations.CancelRegistration;

/// <summary>
/// Represents the endpoint for cancelling an existing registration for a ticketed event.
/// </summary>
public static class CancelRegistrationEndpoint
{
    public static RouteGroupBuilder MapCancelRegistration(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/{registrationId:guid}", CancelRegistration)
            .WithName(nameof(CancelRegistration))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> CancelRegistration(
        string teamSlug,
        string eventSlug,
        Guid registrationId,
        string signature,
        ISigningService signingService,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        if (!signingService.IsValid(registrationId, signature))
        {
            throw new ApplicationRuleException(ApplicationRuleError.Registration.InvalidSignature);
        }
        
        var registration = await context.AttendeeRegistrations.GetEntityAsync(
            registrationId,
            cancellationToken: cancellationToken);

        var (_, eventId) =
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents.GetEntityAsync(eventId, cancellationToken: cancellationToken);
        
        registration.Cancel(ticketedEvent.CancellationPolicy, ticketedEvent.StartTime);

        return TypedResults.Ok();
    }
}