using Amolenk.Admitto.Core.Registrations.Application.Security;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Http;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ResolvePartnerRegistration.PartnerApi;

public static class ResolvePartnerRegistrationHttpEndpoint
{
    public static RouteGroupBuilder MapResolvePartnerRegistration(this RouteGroupBuilder group)
    {
        group.MapGet("/registrations/resolve", ResolvePartnerRegistration)
            .WithName(nameof(ResolvePartnerRegistration))
            .RequireEmailVerificationBearerToken();

        return group;
    }

    private static async ValueTask<IResult> ResolvePartnerRegistration(
        HttpContext httpContext,
        string eventSlug,
        string email,
        IVerificationTokenService verificationTokenService,
        PartnerTicketedEventResolver eventResolver,
        IQueryHandler<ResolvePartnerRegistrationQuery, PartnerRegistrationResolutionDto?> handler,
        CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.TryFrom(email);
        if (!emailResult.IsSuccess)
            return Errors.InvalidEmail.ToProblemHttpResult();

        var teamId = httpContext.User.GetRequiredTeamId();
        var eventId = await eventResolver.ResolveAsync(TeamId.From(teamId), eventSlug, cancellationToken);

        var bearerToken = ExtractBearerToken(httpContext.Request);
        if (bearerToken is null)
            return Errors.TokenRequired.ToProblemHttpResult();

        var claims = verificationTokenService.Validate(bearerToken, eventId);
        if (claims is null)
            return Errors.TokenInvalid.ToProblemHttpResult();

        if (claims.Email != emailResult.ValueObject)
            return Errors.EmailMismatch.ToProblemHttpResult();

        var result = await handler.HandleAsync(
            new ResolvePartnerRegistrationQuery(teamId, eventId, claims.Email.Value),
            cancellationToken);

        return result is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(result);
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
        public static readonly Error InvalidEmail = new(
            "registration.invalid_email",
            "The provided email address is not valid.");

        public static readonly Error TokenRequired = new(
            "email.verification_required",
            "An email-verification token is required to resolve a registration.",
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
