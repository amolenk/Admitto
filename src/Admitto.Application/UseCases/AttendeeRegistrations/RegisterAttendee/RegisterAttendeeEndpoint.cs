using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Identity;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.AttendeeRegistrations.RegisterAttendee;

/// <summary>
/// Represents the endpoint for creating a new registration for a ticketed event.
/// </summary>
public static class RegisterAttendeeEndpoint
{
    public static RouteGroupBuilder MapRegisterAttendee(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", RegisterAttendee)
            .WithName(nameof(RegisterAttendee))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok<RegisterAttendeeResponse>> RegisterAttendee(
        string teamSlug,
        string eventSlug,
        RegisterAttendeeRequest request,
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

        var registrationId = ticketedEvent.RegisterAttendee(
            email,
            request.FirstName,
            request.LastName,
            request.AdditionalDetails.Select(ad => new AdditionalDetail(ad.Name, ad.Value)).ToList(),
            request.Tickets.Select(t => new TicketSelection(t.TicketTypeSlug, t.Quantity)).ToList());

        return TypedResults.Ok(new RegisterAttendeeResponse(registrationId));
    }
}