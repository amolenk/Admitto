using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CreateTicketedEvent.AdminApi;

public static class CreateTicketedEventHttpEndpoint
{
    public static RouteGroupBuilder MapCreateTicketedEvent(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", CreateTicketedEvent)
            .WithName(nameof(CreateTicketedEvent))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Created> CreateTicketedEvent(
        string teamSlug,
        IOrganizationScopeResolver scopeResolver,
        CreateTicketedEventHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, cancellationToken: cancellationToken);

        var command = request.ToCommand(scope.TeamId);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/teams/{teamSlug}/events/{request.Slug}");
    }
}
