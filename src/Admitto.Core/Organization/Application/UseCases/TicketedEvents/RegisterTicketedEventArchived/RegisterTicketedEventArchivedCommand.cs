using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventArchived;

/// <summary>
/// Advances the owning team's counters when an event is archived in Registrations.
/// Idempotent via <c>Team.RegisterEventArchived</c>.
/// </summary>
internal sealed record RegisterTicketedEventArchivedCommand(
    Guid TeamId,
    Guid TicketedEventId) : Command;
