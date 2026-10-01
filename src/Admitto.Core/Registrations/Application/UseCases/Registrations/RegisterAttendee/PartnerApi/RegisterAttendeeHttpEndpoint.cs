using Amolenk.Admitto.Core.Registrations.Application.Security;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Http;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee.PartnerApi;

public static class RegisterAttendeeHttpEndpoint
{
    public static RouteGroupBuilder MapRegisterAttendee(this RouteGroupBuilder group)
    {
        group.MapPost("/registrations", RegisterAttendee)
            .WithName(nameof(RegisterAttendee))
            .RequireEmailVerificationBearerToken()
            .Produces<RegisterAttendeeTicketStateConflictProblemDetails>(
                StatusCodes.Status409Conflict,
                "application/problem+json");

        return group;
    }

    private static async ValueTask<IResult> RegisterAttendee(
        HttpContext httpContext,
        string eventSlug,
        RegisterAttendeeHttpRequest request,
        IVerificationTokenService verificationTokenService,
        PartnerTicketedEventResolver eventResolver,
        ICommandHandler<RegisterAttendeeCommand, RegisterAttendeeResult> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var eventId = await eventResolver.ResolveAsync(TeamId.From(teamId), eventSlug, cancellationToken);
        var bearerToken = ExtractBearerToken(httpContext.Request);
        if (bearerToken is null)
            return Errors.TokenRequired.ToProblemHttpResult();

        var claims = verificationTokenService.Validate(bearerToken, eventId);
        if (claims is null)
            return Errors.TokenInvalid.ToProblemHttpResult();

        if (claims.Email != EmailAddress.From(request.Email))
            return Errors.EmailMismatch.ToProblemHttpResult();

        var command = new RegisterAttendeeCommand(
            eventId.Value,
            teamId,
            claims.Email.Value,
            request.FirstName,
            request.LastName,
            request.RegisterTicketTypeIds,
            request.WaitlistTicketTypeIds,
            request.AdditionalDetails,
            request.CouponCode);

        var result = await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/events/{eventSlug}/registrations/{result.RegistrationId}",
            new RegisterAttendeeHttpResponse(
                result.RegistrationId,
                result.RegisteredTicketTypeIds,
                result.WaitlistedTicketTypeIds));
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
            "An email-verification token is required for self-service registration.",
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
