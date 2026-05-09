using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEventDetails;

internal sealed record GetTicketedEventDetailsQuery(TicketedEventId EventId)
    : Query<TicketedEventDetailsDto?>;
