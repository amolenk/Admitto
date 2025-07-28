using Amolenk.Admitto.Application.Common.Authorization;

namespace Amolenk.Admitto.Application.UseCases.Registrations.GetRegistrations;

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
        CancellationToken cancellationToken)
    {
        var registration = await context.Registrations.GetEntityAsync(
            registrationId,
            cancellationToken: cancellationToken);

        var response = new GetRegistrationResponse(registration.Email, registration.Status);
        
        return TypedResults.Ok(response);
    }
}