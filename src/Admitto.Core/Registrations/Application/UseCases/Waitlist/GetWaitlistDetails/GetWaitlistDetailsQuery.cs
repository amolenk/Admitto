using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.GetWaitlistDetails;

internal sealed record GetWaitlistDetailsQuery(
    Guid EventId,
    Guid TicketTypeId) : Query<WaitlistDetailsDto?>;
