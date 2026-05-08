using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureRegistrationPolicy.AdminApi;

public static class ConfigureRegistrationPolicyHttpEndpoint
{
    public static RouteGroupBuilder MapConfigureRegistrationPolicy(this RouteGroupBuilder group)
    {
        group
            .MapPut("/registration-policy", ConfigureRegistrationPolicy)
            .WithName(nameof(ConfigureRegistrationPolicy))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> ConfigureRegistrationPolicy(
        Guid teamId,
        Guid eventId,
        ConfigureRegistrationPolicyHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(eventId);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
