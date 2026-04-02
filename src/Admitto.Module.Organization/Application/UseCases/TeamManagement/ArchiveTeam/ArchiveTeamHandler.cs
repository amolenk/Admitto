using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.ArchiveTeam;

/// <summary>
/// Implements US-005: archives a team, guarding against active ticketed events.
/// </summary>
/// <remarks>
/// <para>
/// The handler uses optimistic concurrency to prevent a TOCTOU race between the active-events
/// check and the archive commit:
/// </para>
/// <list type="number">
///   <item>
///     The client reads the team (receiving its current EF <c>Version</c> token) and
///     passes it as <c>ExpectedVersion</c> on the archive request.
///   </item>
///   <item>
///     <c>GetAsync</c> validates that <c>ExpectedVersion</c> still matches the current
///     <c>Version</c> in the database; a mismatch throws a concurrency conflict immediately.
///   </item>
///   <item>
///     After the active-events guard passes, <c>team.Archive()</c> is called. If a concurrent
///     <c>CreateTicketedEvent</c> committed between steps 1 and 3, it will have incremented
///     <c>Team.TicketedEventScopeVersion</c>, which advances the <c>Version</c> token. The
///     subsequent <c>SaveChanges</c> then detects the conflict and fails, so the archive is
///     not persisted.
///   </item>
/// </list>
/// </remarks>
internal sealed class ArchiveTeamHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<ArchiveTeamCommand>
{
    public async ValueTask HandleAsync(ArchiveTeamCommand command, CancellationToken cancellationToken)
    {
        var team = await writeStore.Teams.GetAsync(
            TeamId.From(command.TeamId),
            command.ExpectedVersion,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var hasActiveEvents = await writeStore.TicketedEvents
            .AsNoTracking()
            .AnyAsync(e => e.TeamId == team.Id && e.EventWindow.End > now, cancellationToken);

        if (hasActiveEvents)
        {
            throw new BusinessRuleViolationException(Errors.HasActiveEvents);
        }

        team.Archive(now);
    }

    internal static class Errors
    {
        public static Error HasActiveEvents =>
            new("team.has_active_events",
                "The team has active ticketed events.",
                Type: ErrorType.Validation);
    }
}
