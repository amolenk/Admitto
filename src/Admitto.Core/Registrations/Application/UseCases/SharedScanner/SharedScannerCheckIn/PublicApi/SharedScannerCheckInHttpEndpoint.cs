using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.ScannerLinks;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CheckIn;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.SharedScanner.SharedScannerCheckIn.PublicApi;

public static class SharedScannerCheckInHttpEndpoint
{
    public static RouteGroupBuilder MapSharedScannerCheckIn(this RouteGroupBuilder group)
    {
        group
            .MapPost("/check-in", CheckIn)
            .WithName("SharedScannerCheckIn")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async ValueTask<Ok<CheckInResponse>> CheckIn(
        string secret,
        SharedScannerCheckInHttpRequest request,
        IRegistrationsWriteStore writeStore,
        ICommandHandler<CheckInCommand, CheckInResponse> handler,
        TimeProvider timeProvider,
        [FromKeyedServices(RegistrationsModule.Key)] IUnitOfWork unitOfWork,
        IServiceScopeFactory scopeFactory,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await SharedScannerAccess.ResolveActiveEventAsync(
            writeStore, secret, timeProvider.GetUtcNow(), cancellationToken);

        var teamId = ticketedEvent.TeamId.Value;
        var eventId = ticketedEvent.Id.Value;

        var result = await handler.HandleAsync(
            new CheckInCommand(teamId, eventId, request.Credential, CheckInSource.SharedScanner),
            cancellationToken);

        result = await CheckInCommitter.CommitAsync(
            result, teamId, eventId, unitOfWork, scopeFactory, cancellationToken);

        return TypedResults.Ok(result);
    }
}
