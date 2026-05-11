using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEventEmailContext;

internal record GetTicketedEventEmailContextQuery(Guid TicketedEventId, Guid RegistrationId)
    : Query<TicketedEventEmailContextDto>;
