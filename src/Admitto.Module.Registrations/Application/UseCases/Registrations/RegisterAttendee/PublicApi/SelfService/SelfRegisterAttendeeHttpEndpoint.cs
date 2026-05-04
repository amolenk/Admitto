using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Application.Security;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.SelfService;

public static class SelfRegisterAttendeeHttpEndpoint
{
    public static RouteGroupBuilder MapSelfRegisterAttendee(this RouteGroupBuilder group)
    {
        group.MapPost("/registrations", HandleAsync)
            .WithName(nameof(SelfRegisterAttendeeHttpEndpoint));

        return group;
    }

    private static async ValueTask<IResult> HandleAsync(
        string teamSlug,
        string eventSlug,
        SelfRegisterAttendeeHttpRequest request,
        HttpRequest httpRequest,
        IOrganizationFacade facade,
        ITicketedEventIdLookup ticketedEventIdLookup,
        IVerificationTokenService verificationTokenService,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = await facade.GetTeamIdAsync(teamSlug, cancellationToken);
        var eventId = await ticketedEventIdLookup.GetTicketedEventIdAsync(teamId, eventSlug, cancellationToken);

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
            TicketedEventId.From(eventId),
            claims.Email,
            FirstName.From(request.FirstName),
            LastName.From(request.LastName),
            request.TicketTypeSlugs,
            RegistrationMode.SelfService,
            CouponCode: null,
            EmailVerificationToken: bearerToken,
            AdditionalDetails: request.AdditionalDetails);

        var registrationId = await mediator.SendReceiveAsync<RegisterAttendeeCommand, RegistrationId>(
            command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId.Value}",
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
