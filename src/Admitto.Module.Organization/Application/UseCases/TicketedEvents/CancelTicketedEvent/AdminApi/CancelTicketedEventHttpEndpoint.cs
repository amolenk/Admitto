using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CancelTicketedEvent.AdminApi;

public static class CancelTicketedEventHttpEndpoint
{
    public static RouteGroupBuilder MapCancelTicketedEvent(this RouteGroupBuilder group)
    {
        group
            .MapPost("/cancel", CancelTicketedEvent)
            .WithName(nameof(CancelTicketedEvent))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> CancelTicketedEvent(
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        CancelTicketedEventHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var command = request.ToCommand(scope.TeamId, scope.EventId!.Value);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
