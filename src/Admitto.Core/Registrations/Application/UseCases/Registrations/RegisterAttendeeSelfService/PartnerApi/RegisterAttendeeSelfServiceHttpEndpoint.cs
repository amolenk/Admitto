using Amolenk.Admitto.Core.Registrations.Application.Security;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Http;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService.PartnerApi;

public static class RegisterAttendeeSelfServiceHttpEndpoint
{
    public static RouteGroupBuilder MapRegisterAttendeeSelfService(this RouteGroupBuilder group)
    {
        group.MapPost("/registrations", RegisterAttendeeSelfService)
            .WithName(nameof(RegisterAttendeeSelfService))
            .RequireEmailVerificationBearerToken()
            .Produces<RegisterAttendeeSelfServiceTicketStateConflictProblemDetails>(
                StatusCodes.Status409Conflict,
                "application/problem+json");

        return group;
    }

    private static async ValueTask<IResult> RegisterAttendeeSelfService(
        HttpContext httpContext,
        string eventSlug,
        RegisterAttendeeSelfServiceHttpRequest request,
        IVerificationTokenService verificationTokenService,
        PartnerTicketedEventResolver eventResolver,
        ICommandHandler<RegisterAttendeeSelfServiceCommand, RegisterAttendeeSelfServiceResult> handler,
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

        var command = new RegisterAttendeeSelfServiceCommand(
            eventId.Value,
            teamId,
            claims.Email.Value,
            request.FirstName,
            request.LastName,
            request.RegisterTicketTypeIds,
            request.WaitlistTicketTypeIds,
            AdditionalDetails: request.AdditionalDetails);

        var result = await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new RegisterAttendeeSelfServiceHttpResponse(
            result.RegistrationId,
            result.RegisteredTicketTypeIds,
            result.WaitlistedTicketTypeIds);

        return Results.Created(
            result.RegistrationId is { } registrationId
                ? $"/api/events/{eventSlug}/registrations/{registrationId}"
                : $"/api/events/{eventSlug}/registrations",
            response);
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
