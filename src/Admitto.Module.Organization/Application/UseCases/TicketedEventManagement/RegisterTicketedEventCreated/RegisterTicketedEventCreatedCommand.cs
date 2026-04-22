using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreated;

/// <summary>
/// Advances the owning team's creation request to <c>Created</c> and swaps counters
/// (<c>Pending → Active</c>) when Registrations confirms materialisation. Idempotent
/// via <c>Team.RegisterEventCreated</c>.
/// </summary>
internal sealed record RegisterTicketedEventCreatedCommand(
    Guid TeamId,
    Guid CreationRequestId,
    Guid TicketedEventId) : Command;
