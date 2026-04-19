using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.DomainEvents;

/// <summary>
/// Raised by <see cref="Entities.TicketedEvent"/> when a new ticketed event is created for a team.
/// </summary>
/// <remarks>
/// Consumed by <c>TicketedEventCreatedDomainEventHandler</c>, which:
/// <list type="bullet">
///   <item>increments <see cref="Entities.Team.TicketedEventScopeVersion"/> on the owning team
///         (closing the TOCTOU window between the active-events guard in
///         <c>ArchiveTeamHandler</c> and its final commit), and</item>
///   <item>publishes the cross-module <c>TicketedEventCreatedModuleEvent</c> via
///         <c>OrganizationMessagePolicy</c> so that other modules (e.g. Registrations)
///         can initialise their own per-event state.</item>
/// </list>
/// </remarks>
public sealed record TicketedEventCreatedDomainEvent(TeamId TeamId, TicketedEventId TicketedEventId) : DomainEvent;
