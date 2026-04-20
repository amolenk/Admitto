using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.SetCancellationPolicy.AdminApi;

public static class SetCancellationPolicyHttpEndpoint
{
    public static RouteGroupBuilder MapSetCancellationPolicy(this RouteGroupBuilder group)
    {
        group
            .MapPut("/cancellation-policy", SetCancellationPolicy)
            .WithName(nameof(SetCancellationPolicy))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> SetCancellationPolicy(
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        SetCancellationPolicyHttpRequest request,
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
