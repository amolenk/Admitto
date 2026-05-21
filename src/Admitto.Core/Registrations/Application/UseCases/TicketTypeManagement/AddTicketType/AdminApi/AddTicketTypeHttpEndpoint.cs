using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType.AdminApi;

public static class AddTicketTypeHttpEndpoint
{
    public static RouteGroupBuilder MapAddTicketType(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", AddTicketType)
            .WithName(nameof(AddTicketType))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Created<AddTicketTypeHttpResponse>> AddTicketType(
        Guid teamId,
        Guid eventId,
        AddTicketTypeHttpRequest request,
        ICommandHandler<AddTicketTypeCommand, Guid> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(eventId);

        var id = await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/teams/{teamId}/events/{eventId}/ticket-types/{id}",
            new AddTicketTypeHttpResponse(id));
    }
}
