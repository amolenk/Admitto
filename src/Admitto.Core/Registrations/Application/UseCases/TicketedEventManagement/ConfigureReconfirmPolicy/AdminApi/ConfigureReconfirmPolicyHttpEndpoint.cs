using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy.AdminApi;

public static class ConfigureReconfirmPolicyHttpEndpoint
{
    public static RouteGroupBuilder MapConfigureReconfirmPolicy(this RouteGroupBuilder group)
    {
        group
            .MapPut("/reconfirm-policy", ConfigureReconfirmPolicy)
            .WithName(nameof(ConfigureReconfirmPolicy))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> ConfigureReconfirmPolicy(
        Guid teamId,
        Guid eventId,
        ConfigureReconfirmPolicyHttpRequest request,
        ConfigureReconfirmPolicyHandler handler,
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
