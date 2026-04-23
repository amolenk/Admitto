using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventEmailContext;

internal record GetTicketedEventEmailContextQuery(Guid TicketedEventId)
    : Query<TicketedEventEmailContextDto>;
