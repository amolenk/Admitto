using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.AdminApi;

public static class SetRegistrationPolicyHttpEndpoint
{
    public static RouteGroupBuilder MapSetRegistrationPolicy(this RouteGroupBuilder group)
    {
        group
            .MapPut("/registration-policy", SetRegistrationPolicy)
            .WithName(nameof(SetRegistrationPolicy))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> SetRegistrationPolicy(
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        SetRegistrationPolicyHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var command = request.ToCommand(TicketedEventId.From(scope.EventId!.Value));

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
