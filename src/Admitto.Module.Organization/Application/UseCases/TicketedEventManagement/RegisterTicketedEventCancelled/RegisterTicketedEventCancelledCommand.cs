using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCancelled;

/// <summary>
/// Advances the owning team's counters when an active event is cancelled in Registrations.
/// Idempotent via <c>Team.RegisterEventCancelled</c>.
/// </summary>
internal sealed record RegisterTicketedEventCancelledCommand(
    Guid TeamId,
    Guid TicketedEventId) : Command;
