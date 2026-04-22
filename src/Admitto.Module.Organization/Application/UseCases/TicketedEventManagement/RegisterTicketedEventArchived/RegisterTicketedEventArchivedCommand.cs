using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventArchived;

/// <summary>
/// Advances the owning team's counters when an event is archived in Registrations.
/// Idempotent via <c>Team.RegisterEventArchived</c>.
/// </summary>
internal sealed record RegisterTicketedEventArchivedCommand(
    Guid TeamId,
    Guid TicketedEventId) : Command;
