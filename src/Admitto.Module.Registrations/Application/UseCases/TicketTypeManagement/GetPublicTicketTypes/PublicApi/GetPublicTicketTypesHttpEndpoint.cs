using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
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
        Guid teamId,
        Guid eventId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetPublicTicketTypesQuery(TicketedEventId.From(eventId));

        var result = await mediator.QueryAsync<GetPublicTicketTypesQuery, IReadOnlyList<PublicTicketTypeDto>>(
            query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
