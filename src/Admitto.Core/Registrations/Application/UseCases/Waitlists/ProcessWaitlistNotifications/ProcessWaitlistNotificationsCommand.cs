using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;

internal sealed record ProcessWaitlistNotificationsCommand(
    Guid EventId,
    Guid TicketTypeId,
    int FreedSlots) : Command;
