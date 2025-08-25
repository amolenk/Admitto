using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Composing;
using Amolenk.Admitto.Application.Common.Identity;
using Amolenk.Admitto.Application.UseCases.Email.SendEmail;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.EmailVerification.RequestOtpCode;

/// <summary>
/// Represents an endpoint for requesting an OTP code to be sent to an email address for verification.
/// </summary>
public static class RequestOtpCodeEndpoint
{
    public static RouteGroupBuilder MapRequestOtp(this RouteGroupBuilder group)
    {
        group
            .MapPost("/requests", RequestOtp)
            .WithName(nameof(RequestOtp))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Accepted> RequestOtp(
        string teamSlug,
        string eventSlug,
        RequestOtpCodeRequest request,
        ISigningService signingService,
        ISlugResolver slugResolver,
        IApplicationContext context,
        IMessageOutbox messageOutbox,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        var email = request.Email.NormalizeEmail();
        
        var existingRequests = await context.EmailVerificationRequests
            .Where(r => r.Email == email)
            .ToListAsync(cancellationToken: cancellationToken);

        // Remove all existing requests for the same email address.
        foreach (var existingRequest in existingRequests)
        {
            context.EmailVerificationRequests.Remove(existingRequest);
        }
        
        var verificationRequest = EmailVerificationRequest.Create(
            eventId,
            email,
            signingService,
            out var code);

        context.EmailVerificationRequests.Add(verificationRequest);

        var sendEmailCommand = new SendEmailCommand(
            teamId,
            eventId,
            verificationRequest.Id,
            EmailType.VerifyEmail,
            new Dictionary<string, string>
            {
                { VerificationEmailComposer.VerificationCodeParameterName, code }
            });

        messageOutbox.Enqueue(sendEmailCommand);

        return TypedResults.Accepted(default(string));
    }
}