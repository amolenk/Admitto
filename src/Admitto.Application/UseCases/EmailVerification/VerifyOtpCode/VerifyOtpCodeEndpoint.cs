using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Cryptography;
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

    private static async ValueTask<Results<Ok<VerifyOtpCodeResponse>, ProblemHttpResult>> VerifyOtpCode(
        string teamSlug,
        string eventSlug,
        VerifyOtpCodeRequest request,
        ISigningService signingService,
        ISlugResolver slugResolver,
        IApplicationContext context,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var verificationRequest = await context.EmailVerificationRequests
            .FirstOrDefaultAsync(
                r => r.Email == request.Email.ToLowerInvariant().Trim(),
                cancellationToken: cancellationToken);

        if (verificationRequest is not null)
        {
            // Remove the verification request regardless of its validity to prevent reuse.
            context.EmailVerificationRequests.Remove(verificationRequest);
        }
        
        var isValid = verificationRequest?.ExpiresAt > DateTime.UtcNow
                      && verificationRequest.Verify(request.Code, signingService);

        if (isValid)
        {
            var token = new EmailVerifiedToken(request.Email, DateTime.UtcNow);
            var response = new VerifyOtpCodeResponse(token.Encode(signingService));
            return TypedResults.Ok(response);

        }

        var exception = new ApplicationRuleException(ApplicationRuleError.EmailVerificationRequest.Invalid);
        var problemDetails = exception.ToProblemDetails(httpContext);
        return TypedResults.Problem(problemDetails);
    }
}