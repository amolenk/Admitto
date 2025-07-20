using Amolenk.Admitto.Application.Common.Authorization;

namespace Amolenk.Admitto.Application.UseCases.PendingRegistrations.GetPendingRegistration;

public static class GetPendingRegistrationEndpoint
{
    public static RouteGroupBuilder MapGetPendingRegistration(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{registrationId:guid}", GetPendingRegistration)
            .WithName(nameof(GetPendingRegistration))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<GetPendingRegistrationResponse>> GetPendingRegistration(
        string teamSlug,
        string eventSlug,
        Guid registrationId,
        IDomainContext context,
        CancellationToken cancellationToken)
    {
        var registration = await context.PendingRegistrations.GetPendingRegistrationAsync(
            registrationId,
            cancellationToken: cancellationToken);

        var response = new GetPendingRegistrationResponse(registration.Email, registration.Status);
        
        return TypedResults.Ok(response);
    }
}