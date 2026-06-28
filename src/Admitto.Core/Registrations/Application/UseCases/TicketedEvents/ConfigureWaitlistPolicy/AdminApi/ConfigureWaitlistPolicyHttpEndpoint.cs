using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureWaitlistPolicy.AdminApi;

public static class ConfigureWaitlistPolicyHttpEndpoint
{
    public static RouteGroupBuilder MapConfigureWaitlistPolicy(this RouteGroupBuilder group)
    {
        group
            .MapPut("/waitlist-policy", ConfigureWaitlistPolicy)
            .WithName(nameof(ConfigureWaitlistPolicy))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> ConfigureWaitlistPolicy(
        Guid teamId,
        Guid eventId,
        ConfigureWaitlistPolicyHttpRequest request,
        ICommandHandler<ConfigureWaitlistPolicyCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(eventId, teamId);

        await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
