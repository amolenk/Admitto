using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Development;

internal enum LocalDemoSeedRequestDecision
{
    Create,
    AlreadyInFlight,
    AlreadyCreated,
    Terminal
}

internal static class LocalDemoSeedRequestState
{
    public static LocalDemoSeedRequestDecision Decide(
        TeamEventCreationRequest? request,
        Slug demoSlug)
    {
        if (request is null)
            return LocalDemoSeedRequestDecision.Create;

        if (request.PublicSlug != demoSlug)
            return LocalDemoSeedRequestDecision.Create;

        if (request.Status == TeamEventCreationRequestStatus.Pending)
            return LocalDemoSeedRequestDecision.AlreadyInFlight;

        if (request.Status == TeamEventCreationRequestStatus.Created
            && request.TicketedEventId.HasValue
            && request.ObservedEventStatus != EventStatus.Archived)
            return LocalDemoSeedRequestDecision.AlreadyCreated;

        return LocalDemoSeedRequestDecision.Terminal;
    }
}
