using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Verification;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Application.Projections.Participation;

namespace Amolenk.Admitto.Application.UseCases.Public.VerifyOtpCode;

/// <summary>
/// Represents the endpoint for verifying an OTP code sent to an email address.
/// The endpoint returns a token that can be used for a subsequent registration request.
/// </summary>
public static class VerifyOtpCodeEndpoint
{
    public static RouteGroupBuilder MapVerifyOtpCode(this RouteGroupBuilder group)
    {
        group
            .MapPost("/verify", VerifyOtpCode)
            .WithName(nameof(VerifyOtpCode));

        return group;
    }

    private static async ValueTask<Ok<VerifyOtpCodeResponse>> VerifyOtpCode(
        string teamSlug,
        string eventSlug,
        VerifyOtpCodeRequest request,
        ISlugResolver slugResolver,
        ISigningService signingService,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);
        var email = request.Email.NormalizeEmail();

        // Find the verification request for the provided email address.
        var verificationRequest = await context.EmailVerificationRequests
            .FirstOrDefaultAsync(
                evr => evr.TicketedEventId == eventId && evr.Email == email,
                cancellationToken: cancellationToken);

        // If we can't find any, throw an exception.
        if (verificationRequest is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.EmailVerificationRequest.Invalid);
        }

        // Verify the code and expiration.
        var isValid = verificationRequest.ExpiresAt > DateTime.UtcNow
                      && await verificationRequest.VerifyAsync(
                          request.Code,
                          eventId,
                          signingService,
                          cancellationToken);

        // Remove the verification request to prevent reuse.
        context.EmailVerificationRequests.Remove(verificationRequest);

        // If the verification failed, throw an exception.
        if (!isValid)
        {
            throw new ApplicationRuleException(ApplicationRuleError.EmailVerificationRequest.Invalid);
        }
        
        var token = new EmailVerifiedToken(email, DateTime.UtcNow);
        var signedToken = await token.EncodeAsync(signingService, eventId, cancellationToken);
        var response = new VerifyOtpCodeResponse(signedToken);

        // Check if the user is already registered.
        var participation = await context.ParticipationView
            .AsNoTracking()
            .Where(p => p.TicketedEventId == eventId && p.Email == email)
            .Select(p => new
            {
                p.PublicId,
                p.AttendeeStatus
            })
            .FirstOrDefaultAsync(cancellationToken);

        // If they are, include their public ID and signature in the response.
        if (participation?.AttendeeStatus == ParticipationAttendeeStatus.Registered)
        {
            response = response with
            {
                PublicId = participation.PublicId,
                Signature = await signingService.SignAsync(participation.PublicId, eventId, cancellationToken)
            };
        }

        return TypedResults.Ok(response);
    }
}