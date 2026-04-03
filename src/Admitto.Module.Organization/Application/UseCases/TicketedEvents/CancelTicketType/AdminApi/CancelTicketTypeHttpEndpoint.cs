using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CancelTicketType.AdminApi;

public static class CancelTicketTypeHttpEndpoint
{
    public static RouteGroupBuilder MapCancelTicketType(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{ticketTypeSlug}/cancel", CancelTicketType)
            .WithName(nameof(CancelTicketType))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> CancelTicketType(
        OrganizationScope organizationScope,
        string ticketTypeSlug,
        CancelTicketTypeHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(
            organizationScope.TeamId,
            organizationScope.EventId!.Value,
            ticketTypeSlug);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
