using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.CancelTicketType.AdminApi;

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

    private static async ValueTask<NoContent> CancelTicketType(
        string ticketTypeSlug,
        OrganizationScope organizationScope,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new CancelTicketTypeCommand(
            TicketedEventId.From(organizationScope.EventId!.Value),
            Slug.From(ticketTypeSlug));

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
