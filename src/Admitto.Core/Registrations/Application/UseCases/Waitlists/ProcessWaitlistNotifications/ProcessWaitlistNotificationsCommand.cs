using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;

/// <summary>
/// Re-evaluates a ticket type's waitlist: offers the seats the catalog has available to the front of the queue, then
/// re-evaluates WaitlistMode.
/// </summary>
internal sealed record ProcessWaitlistNotificationsCommand(
    Guid EventId,
    Guid TeamId,
    Guid TicketTypeId) : Command;
