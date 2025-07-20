using Amolenk.Admitto.Application.Common.Authorization;

namespace Amolenk.Admitto.Application.UseCases.PendingRegistrations.VerifyRegistrationRequest;

public static class VerifyPendingRegistrationEndpoint
{
    public static RouteGroupBuilder MapVerifyPendingRegistration(this RouteGroupBuilder group)
    {
        group
            .MapPut("/{registrationId:guid}/verify", VerifyPendingRegistration)
            .WithName(nameof(VerifyPendingRegistration))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> VerifyPendingRegistration(
        string teamSlug,
        string eventSlug,
        Guid registrationId,
        VerifyPendingRegistrationRequest request,
        IDomainContext context,
        CancellationToken cancellationToken)
    {
        var registration = await context.PendingRegistrations.GetPendingRegistrationAsync(
            registrationId,
            cancellationToken: cancellationToken);

        registration.Verify(request.Code);

        return TypedResults.Ok();
    }
}