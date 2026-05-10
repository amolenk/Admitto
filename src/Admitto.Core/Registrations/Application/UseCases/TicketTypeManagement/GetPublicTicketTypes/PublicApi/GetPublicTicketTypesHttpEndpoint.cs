using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.GetPublicTicketTypes.PublicApi;

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
        GetPublicTicketTypesHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetPublicTicketTypesQuery(TicketedEventId.From(eventId));

        var result = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
