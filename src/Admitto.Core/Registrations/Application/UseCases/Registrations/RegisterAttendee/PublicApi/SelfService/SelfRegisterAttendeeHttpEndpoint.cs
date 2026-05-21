using Amolenk.Admitto.Core.Registrations.Application.Security;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.SelfService;

public static class SelfRegisterAttendeeHttpEndpoint
{
    public static RouteGroupBuilder MapSelfRegisterAttendee(this RouteGroupBuilder group)
    {
        group.MapPost("/registrations", HandleAsync)
            .WithName(nameof(SelfRegisterAttendeeHttpEndpoint));

        return group;
    }

    private static async ValueTask<IResult> HandleAsync(
        Guid teamId,
        Guid eventId,
        SelfRegisterAttendeeHttpRequest request,
        HttpRequest httpRequest,
        IVerificationTokenService verificationTokenService,
        ICommandHandler<RegisterAttendeeCommand, Guid> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var bearerToken = ExtractBearerToken(httpRequest);
        if (bearerToken is null)
            return Results.Problem(
                detail: "An email-verification token is required for self-service registration.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: new Dictionary<string, object?> { ["code"] = "email.verification_required" });

        var claims = verificationTokenService.Validate(bearerToken, TicketedEventId.From(eventId));
        if (claims is null)
            return Results.Problem(
                detail: "The email-verification token is invalid or expired.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: new Dictionary<string, object?> { ["code"] = "email.verification_invalid" });

        var command = new RegisterAttendeeCommand(
            eventId,
            claims.Email.Value,
            request.FirstName,
            request.LastName,
            request.TicketTypeIds,
            RegistrationMode.SelfService,
            CouponCode: null,
            EmailVerificationToken: bearerToken,
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
}
