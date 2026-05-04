using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.GetPublicTicketTypes.PublicApi;

public static class GetPublicTicketTypesHttpEndpoint
{
    public static RouteGroupBuilder MapGetPublicTicketTypes(this RouteGroupBuilder group)
    {
        group.MapGet("/ticket-types", HandleAsync)
            .WithName(nameof(GetPublicTicketTypesHttpEndpoint));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<PublicTicketTypeDto>>> HandleAsync(
        string teamSlug,
        string eventSlug,
        IOrganizationFacade facade,
        ITicketedEventIdLookup ticketedEventIdLookup,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var teamId = await facade.GetTeamIdAsync(teamSlug, cancellationToken);
        var eventIdGuid = await ticketedEventIdLookup.GetTicketedEventIdAsync(teamId, eventSlug, cancellationToken);
        var eventId = TicketedEventId.From(eventIdGuid);

        var query = new GetPublicTicketTypesQuery(eventId);

        var result = await mediator.QueryAsync<GetPublicTicketTypesQuery, IReadOnlyList<PublicTicketTypeDto>>(
            query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
