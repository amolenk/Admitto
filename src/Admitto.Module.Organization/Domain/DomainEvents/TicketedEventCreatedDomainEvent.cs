using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.DomainEvents;

/// <summary>
/// Raised by <see cref="Entities.TicketedEvent"/> when a new ticketed event is created for a team.
/// </summary>
/// <remarks>
/// Consumed by <c>TicketedEventCreatedDomainEventHandler</c>, which increments
/// <see cref="Entities.Team.TicketedEventScopeVersion"/> on the owning team. This keeps the
/// team row's EF concurrency token (<c>Version</c>) in sync whenever a ticketed event is
/// registered, closing the TOCTOU window between the active-events guard in
/// <c>ArchiveTeamHandler</c> and its final commit.
/// </remarks>
public sealed record TicketedEventCreatedDomainEvent(TeamId TeamId) : DomainEvent;
