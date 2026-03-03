using Amolenk.Admitto.Shared.Application.Auth;
using Amolenk.Admitto.Shared.Application.Http;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Application.Persistence;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.TicketedEvents.CreateTicketedEvent.AdminApi;

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
        OrganizationScope organizationScope,
        CreateTicketedEventHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(organizationScope.TeamId);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/teams/{organizationScope.TeamSlug}/events/{request.Slug}");
    }
}
