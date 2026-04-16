using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType.AdminApi;

public static class UpdateTicketTypeHttpEndpoint
{
    public static RouteGroupBuilder MapUpdateTicketType(this RouteGroupBuilder group)
    {
        group
            .MapPut("/{ticketTypeSlug}", UpdateTicketType)
            .WithName(nameof(UpdateTicketType))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> UpdateTicketType(
        string ticketTypeSlug,
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        UpdateTicketTypeHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var command = request.ToCommand(
            TicketedEventId.From(scope.EventId!.Value),
            ticketTypeSlug);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
