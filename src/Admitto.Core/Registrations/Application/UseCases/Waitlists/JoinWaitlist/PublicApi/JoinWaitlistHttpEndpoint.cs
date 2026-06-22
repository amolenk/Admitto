using Amolenk.Admitto.Core.Registrations.Application.Security;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Http;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.JoinWaitlist.PublicApi;

public static class JoinWaitlistHttpEndpoint
{
    public static RouteGroupBuilder MapJoinWaitlist(this RouteGroupBuilder group)
    {
        group
            .MapPost("/waitlist/{ticketTypeId:guid}", JoinWaitlist)
            .WithName(nameof(JoinWaitlist));

        return group;
    }

    private static async ValueTask<IResult> JoinWaitlist(
        HttpContext httpContext,
        Guid eventId,
        Guid ticketTypeId,
        JoinWaitlistHttpRequest request,
        IVerificationTokenService verificationTokenService,
        ICommandHandler<JoinWaitlistCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var bearerToken = ExtractBearerToken(httpContext.Request);
        if (bearerToken is null)
            return Errors.TokenRequired.ToProblemHttpResult();

        var claims = verificationTokenService.Validate(bearerToken, TicketedEventId.From(eventId));
        if (claims is null)
            return Errors.TokenInvalid.ToProblemHttpResult();

        if (claims.Email != EmailAddress.From(request.Email))
            return Errors.EmailMismatch.ToProblemHttpResult();

        var command = new JoinWaitlistCommand(
            teamId,
            eventId,
            ticketTypeId,
            claims.Email.Value);

        await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Accepted((string?)null);
    }

    private static string? ExtractBearerToken(HttpRequest request)
    {
        var authHeader = request.Headers.Authorization.FirstOrDefault();
        if (authHeader is null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        return authHeader["Bearer ".Length..].Trim();
    }

    private static class Errors
    {
        public static readonly Error TokenRequired = new(
            "email.verification_required",
            "An email-verification token is required to join the waitlist.",
            Type: ErrorType.Unauthorized);

        public static readonly Error TokenInvalid = new(
            "email.verification_invalid",
            "The email-verification token is invalid or expired.",
            Type: ErrorType.Unauthorized);

        public static readonly Error EmailMismatch = new(
            "email.verification_mismatch",
            "The provided email does not match the verification token.",
            Type: ErrorType.Unauthorized);
    }
}
