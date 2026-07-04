using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.GetWaitlistDetails;

internal sealed record GetWaitlistDetailsQuery(
    Guid EventId,
    Guid TeamId,
    Guid TicketTypeId) : Query<WaitlistDetailsDto?>;
