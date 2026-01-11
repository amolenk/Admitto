using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Verification;
using Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Public.Register;

/// <summary>
/// Represents the public endpoint for creating a new registration for a ticketed event.
/// </summary>
public static class RegisterEndpoint
{
    public static RouteGroupBuilder MapRegister(this RouteGroupBuilder group)
    {
        group
            .MapPost("/register", Register)
            .WithName(nameof(Register));

        return group;
    }

    private static async ValueTask<Created> Register(
        string teamSlug,
        string eventSlug,
        RegisterRequest request,
        ISigningService signingService,
        ISlugResolver slugResolver,
        [FromServices] RegisterAttendeeHandler registerAttendeeHandler,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        var coupons = await EnsureValidRequestAsync(request, eventId, signingService, cancellationToken);

        var command = new RegisterAttendeeCommand(
            teamId,
            eventId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.AdditionalDetails.Select(ad => new AdditionalDetail(ad.Name, ad.Value)).ToList(),
            request.RequestedTickets.Select(t => new TicketSelection(t, 1)).ToList(),
            coupons);

        await registerAttendeeHandler.HandleAsync(command, cancellationToken);

        return TypedResults.Created();
    }

    private static async ValueTask<List<Coupon>> EnsureValidRequestAsync(
        RegisterRequest request,
        Guid eventId,
        ISigningService signingService,
        CancellationToken cancellationToken)
    {
        var email = request.Email.NormalizeEmail();

        // Ensure the email has been verified using the provided token.
        if (request.VerificationToken is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.InvalidVerificationToken);
        }

        var token = await EmailVerifiedToken.TryDecodeAndValidateAsync(
            request.VerificationToken,
            eventId,
            signingService,
            cancellationToken);

        if (token?.Email != email)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.InvalidVerificationToken);
        }

        return token.Coupons ?? [];
    }
}