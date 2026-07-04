using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;

internal sealed record ChangeAttendeeTicketsCommand(
    Guid EventId,
    Guid TeamId,
    Guid RegistrationId,
    IReadOnlyList<Guid> TicketTypeIds,
    ChangeMode Mode,
    Guid? WaitlistCouponCode = null) : Command;
