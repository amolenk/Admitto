using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CreateTicketedEvent;

/// <summary>
/// Reacts to <see cref="TicketedEventCreatedDomainEvent"/> by incrementing
/// <c>Team.TicketedEventScopeVersion</c> on the owning team.
/// </summary>
/// <remarks>
/// This handler runs inside the EF <c>SavingChangesAsync</c> interceptor, so the team update
/// is committed atomically in the same transaction as the new ticketed event row.
///
/// Incrementing <c>TicketedEventScopeVersion</c> forces a write to the team row, which bumps
/// the team's EF row-version concurrency token (<c>Version</c>). Any concurrent
/// <c>ArchiveTeam</c> operation that read the old token will then fail with a concurrency
/// conflict, preventing a race between the active-events guard and the archive commit.
/// </remarks>
internal sealed class TicketedEventCreatedDomainEventHandler(IOrganizationWriteStore writeStore)
    : IDomainEventHandler<TicketedEventCreatedDomainEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var team = await writeStore.Teams.GetAsync(domainEvent.TeamId, cancellationToken);

        team.RegisterTicketedEventCreation();
    }
}
