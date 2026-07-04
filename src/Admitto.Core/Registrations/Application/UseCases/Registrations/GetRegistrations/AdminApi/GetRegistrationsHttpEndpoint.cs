using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations.AdminApi;

public static class GetRegistrationsHttpEndpoint
{
    public static RouteGroupBuilder MapGetRegistrations(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetRegistrations)
            .WithName(nameof(GetRegistrations))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Results<Ok<IReadOnlyList<RegistrationListItemDto>>, NotFound>> GetRegistrations(
        Guid teamId,
        Guid eventId,
        IQueryHandler<GetRegistrationsQuery, IReadOnlyList<RegistrationListItemDto>?> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetRegistrationsQuery(
                TicketedEventId.From(eventId),
                TeamId.From(teamId)),
            cancellationToken);

        if (result is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(result);
    }
}
