using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.SharedScanner.ResolveSharedScannerSession.PublicApi;

public static class ResolveSharedScannerSessionHttpEndpoint
{
    public static RouteGroupBuilder MapResolveSharedScannerSession(this RouteGroupBuilder group)
    {
        group
            .MapGet(string.Empty, ResolveSharedScannerSession)
            .WithName(nameof(ResolveSharedScannerSession))
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async ValueTask<Ok<SharedScannerSessionDto>> ResolveSharedScannerSession(
        string secret,
        IQueryHandler<ResolveSharedScannerSessionQuery, SharedScannerSessionDto> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ResolveSharedScannerSessionQuery(secret), cancellationToken);

        return TypedResults.Ok(result);
    }
}
