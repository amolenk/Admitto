using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Identity;
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
        ISigningService signingService,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var email = request.Email.NormalizeEmail();
        
        // Ensure the email has been verified using the provided token.
        if (!EmailVerifiedToken.TryDecodeAndValidate(
                request.VerificationToken,
                signingService,
                out var verificationToken)
            || verificationToken.Email != email)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Registration.InvalidVerificationToken);
        }
        
        var (_, eventId) =
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents.GetEntityAsync(eventId, cancellationToken: cancellationToken);

        var registrationId = ticketedEvent.Register(
            email,
            request.FirstName,
            request.LastName,
            request.AdditionalDetails.Select(ad => new AdditionalDetail(ad.Name, ad.Value)).ToList(),
            request.Tickets.Select(t => new TicketSelection(t.TicketTypeSlug, t.Quantity)).ToList());

        return TypedResults.Ok(new RegisterResponse(registrationId));
    }
}