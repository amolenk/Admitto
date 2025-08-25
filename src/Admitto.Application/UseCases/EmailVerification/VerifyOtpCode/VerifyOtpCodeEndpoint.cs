using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Authorization;
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
        ISigningService signingService,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var email = request.Email.NormalizeEmail();
        
        var verificationRequest = await context.EmailVerificationRequests
            .FirstOrDefaultAsync(
                r => r.Email == email,
                cancellationToken: cancellationToken);

        var isValid = verificationRequest?.ExpiresAt > DateTime.UtcNow
                      && verificationRequest.Verify(request.Code, signingService);

        if (!isValid)
        {
            throw new ApplicationRuleException(ApplicationRuleError.EmailVerificationRequest.Invalid);
        }
        
        // Remove the verification request to prevent reuse.
        context.EmailVerificationRequests.Remove(verificationRequest!);

        var token = new EmailVerifiedToken(email, DateTime.UtcNow);
        var response = new VerifyOtpCodeResponse(token.Encode(signingService));
        return TypedResults.Ok(response);
    }
}