using Amolenk.Admitto.Core.Registrations.Application.Security;
using Amolenk.Admitto.Core.Shared.Application.Http;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService.PublicApi;

public static class RegisterAttendeeSelfServiceHttpEndpoint
{
    public static RouteGroupBuilder MapRegisterAttendeeSelfService(this RouteGroupBuilder group)
    {
        group.MapPost("/registrations", RegisterAttendeeSelfService)
            .WithName(nameof(RegisterAttendeeSelfService));

        return group;
    }

    private static async ValueTask<IResult> RegisterAttendeeSelfService(
        Guid teamId,
        Guid eventId,
        RegisterAttendeeSelfServiceHttpRequest request,
        HttpRequest httpRequest,
        IVerificationTokenService verificationTokenService,
        ICommandHandler<RegisterAttendeeSelfServiceCommand, Guid> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var bearerToken = ExtractBearerToken(httpRequest);
        if (bearerToken is null)
            return Errors.TokenRequired.ToProblemHttpResult();

        var claims = verificationTokenService.Validate(bearerToken, TicketedEventId.From(eventId));
        if (claims is null)
            return Errors.TokenInvalid.ToProblemHttpResult();

        if (claims.Email != EmailAddress.From(request.Email))
            return Errors.EmailMismatch.ToProblemHttpResult();

        var command = new RegisterAttendeeSelfServiceCommand(
            eventId,
            claims.Email.Value,
            request.FirstName,
            request.LastName,
            request.TicketTypeIds,
            AdditionalDetails: request.AdditionalDetails);

        var registrationId = await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/teams/{teamId}/events/{eventId}/registrations/{registrationId}",
            null);
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
