using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Cryptography;

namespace Amolenk.Admitto.Application.UseCases.Registrations.GetRegistration;

public static class GetRegistrationEndpoint
{
    public static RouteGroupBuilder MapGetRegistration(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{registrationId:guid}", GetRegistration)
            .WithName(nameof(GetRegistration))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<GetRegistrationResponse>> GetRegistration(
        string teamSlug,
        string eventSlug,
        Guid registrationId,
        IApplicationContext context,
        ISigningService signingService,
        CancellationToken cancellationToken)
    {
        var registration = await context.Registrations.GetEntityAsync(
            registrationId,
            cancellationToken: cancellationToken);

        var signature = signingService.Sign(registrationId);
        
        var response = new GetRegistrationResponse(registration.Email, registration.Status, signature);
        
        return TypedResults.Ok(response);
    }
}