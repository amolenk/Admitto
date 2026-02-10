using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Registrations.Application.UseCases.RegisterAttendee;

internal record RegisterAttendeeCommand(
    TicketedEventId EventId,
    EmailAddress EmailAddress,
    AttendeeInfo AttendeeInfo,
    TicketRequest[] TicketRequests) : Command<RegistrationId>;
    
