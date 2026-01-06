using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Composing;
using Amolenk.Admitto.Application.Common.Email.Verification;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Application.UseCases.Email.SendEmail;

namespace Amolenk.Admitto.Application.UseCases.Public.RequestOtpCode;

/// <summary>
/// Represents an endpoint for requesting an OTP code to be sent to an email address for verification.
/// </summary>
public static class RequestOtpCodeEndpoint
{
    public static RouteGroupBuilder MapRequestOtp(this RouteGroupBuilder group)
    {
        group
            .MapPost("/otp", RequestOtp)
            .WithName(nameof(RequestOtp));

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
        var (teamId, eventId) = await slugResolver.ResolveTeamAndTicketedEventIdsAsync(
            teamSlug,
            eventSlug,
            cancellationToken);

        var email = request.Email.NormalizeEmail();

        var existingRequests = await context.EmailVerificationRequests
            .Where(evr => evr.TicketedEventId == eventId && evr.Email == email)
            .ToListAsync(cancellationToken);

        // Remove all existing requests for the same email address.
        foreach (var existingRequest in existingRequests)
        {
            context.EmailVerificationRequests.Remove(existingRequest);
        }

        var (verificationRequest, code) = await EmailVerificationRequest.CreateAsync(
            eventId,
            email,
            signingService,
            cancellationToken);

        context.EmailVerificationRequests.Add(verificationRequest);

        var sendEmailCommand = new SendEmailCommand(
            eventId,
            verificationRequest.Id,
            WellKnownEmailType.VerifyEmail,
            teamId,
            new Dictionary<string, string>
            {
                { VerificationEmailComposer.VerificationCodeParameterName, code }
            });

        messageOutbox.Enqueue(sendEmailCommand);

        return TypedResults.Accepted(default(string));
    }
}