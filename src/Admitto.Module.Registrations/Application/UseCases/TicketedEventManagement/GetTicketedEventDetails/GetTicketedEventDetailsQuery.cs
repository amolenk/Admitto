using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEventDetails;

internal sealed record GetTicketedEventDetailsQuery(TicketedEventId EventId)
    : Query<TicketedEventDetailsDto?>;
