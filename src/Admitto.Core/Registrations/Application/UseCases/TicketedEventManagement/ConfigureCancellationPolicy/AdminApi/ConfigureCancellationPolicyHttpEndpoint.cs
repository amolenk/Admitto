using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy.AdminApi;

public static class ConfigureCancellationPolicyHttpEndpoint
{
    public static RouteGroupBuilder MapConfigureCancellationPolicy(this RouteGroupBuilder group)
    {
        group
            .MapPut("/cancellation-policy", ConfigureCancellationPolicy)
            .WithName(nameof(ConfigureCancellationPolicy))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> ConfigureCancellationPolicy(
        Guid teamId,
        Guid eventId,
        ConfigureCancellationPolicyHttpRequest request,
        ConfigureCancellationPolicyHandler handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(eventId);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
