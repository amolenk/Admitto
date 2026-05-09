using Amolenk.Admitto.Core.Module.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventEmailContext;

internal record GetTicketedEventEmailContextQuery(Guid TicketedEventId, Guid RegistrationId)
    : Query<TicketedEventEmailContextDto>;
