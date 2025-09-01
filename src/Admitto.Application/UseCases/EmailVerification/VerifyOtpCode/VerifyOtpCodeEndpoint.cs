using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Identity;

namespace Amolenk.Admitto.Application.UseCases.EmailVerification.VerifyOtpCode;

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
            .WithName(nameof(VerifyOtpCode))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

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
        
        var verificationRequest = await context.EmailVerificationRequests
            .FirstOrDefaultAsync(
                evr => evr.TicketedEventId == eventId && evr.Email == email,
                cancellationToken: cancellationToken);

        var isValid = verificationRequest?.ExpiresAt > DateTime.UtcNow
                      && await verificationRequest.VerifyAsync(request.Code, eventId, signingService, cancellationToken);

        if (!isValid)
        {
            throw new ApplicationRuleException(ApplicationRuleError.EmailVerificationRequest.Invalid);
        }
        
        // Remove the verification request to prevent reuse.
        context.EmailVerificationRequests.Remove(verificationRequest!);

        var token = new EmailVerifiedToken(email, DateTime.UtcNow);
        var signedToken = await token.EncodeAsync(signingService, eventId, cancellationToken);
        var response = new VerifyOtpCodeResponse(signedToken);
        return TypedResults.Ok(response);
    }
}