using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.ScannerLinks;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.LookupCheckInCandidates;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.SharedScanner.SharedScannerLookup.PublicApi;

public static class SharedScannerLookupHttpEndpoint
{
    public static RouteGroupBuilder MapSharedScannerLookup(this RouteGroupBuilder group)
    {
        group
            .MapGet("/lookup", Lookup)
            .WithName("SharedScannerLookup")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<CheckInLookupCandidateDto>>> Lookup(
        string secret,
        string? query,
        IRegistrationsWriteStore writeStore,
        IQueryHandler<LookupCheckInCandidatesQuery, IReadOnlyList<CheckInLookupCandidateDto>> handler,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await SharedScannerAccess.ResolveActiveEventAsync(
            writeStore, secret, timeProvider.GetUtcNow(), cancellationToken);

        var result = await handler.HandleAsync(
            new LookupCheckInCandidatesQuery(
                ticketedEvent.TeamId.Value, ticketedEvent.Id.Value, query ?? string.Empty),
            cancellationToken);

        return TypedResults.Ok(result);
    }
}
