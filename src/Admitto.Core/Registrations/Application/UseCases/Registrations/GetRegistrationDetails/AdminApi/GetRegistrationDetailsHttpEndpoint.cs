using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrationDetails.AdminApi;

public static class GetRegistrationDetailsHttpEndpoint
{
    public static RouteGroupBuilder MapGetRegistrationDetails(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{registrationId:guid}", GetRegistrationDetails)
            .WithName(nameof(GetRegistrationDetails))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Results<Ok<RegistrationDetailDto>, NotFound>> GetRegistrationDetails(
        Guid teamId,
        Guid eventId,
        Guid registrationId,
        IQueryHandler<GetRegistrationDetailsQuery, RegistrationDetailDto?> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetRegistrationDetailsQuery(
            TeamId: teamId,
            EventId: TicketedEventId.From(eventId),
            RegistrationId: RegistrationId.From(registrationId));

        var result = await handler.HandleAsync(query, cancellationToken);

        if (result is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(result);
    }
}
