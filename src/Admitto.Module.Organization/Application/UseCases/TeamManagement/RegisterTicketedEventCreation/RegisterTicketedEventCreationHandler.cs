using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.RegisterTicketedEventCreation;

/// <summary>
/// Handles <see cref="RegisterTicketedEventCreationCommand"/> by incrementing
/// <c>Team.TicketedEventScopeVersion</c> on the owning team.
/// </summary>
/// <remarks>
/// Called by <c>TicketedEventCreatedDomainEventHandler</c> inside the EF
/// <c>SavingChangesAsync</c> interceptor, so the team update is committed atomically in the
/// same transaction as the new ticketed event row. Incrementing
/// <c>TicketedEventScopeVersion</c> forces a write to the team row, advancing its EF
/// row-version concurrency token. Any concurrent <c>ArchiveTeam</c> operation that held the
/// old token will then fail with a concurrency conflict.
/// </remarks>
internal sealed class RegisterTicketedEventCreationHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<RegisterTicketedEventCreationCommand>
{
    public async ValueTask HandleAsync(
        RegisterTicketedEventCreationCommand command,
        CancellationToken cancellationToken)
    {
        var team = await writeStore.Teams.GetAsync(command.TeamId, cancellationToken);

        team.RegisterTicketedEventCreation();
    }
}
