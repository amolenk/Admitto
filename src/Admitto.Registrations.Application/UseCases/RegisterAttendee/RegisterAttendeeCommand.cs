using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Registrations.Application.UseCases.RegisterAttendee;

internal record RegisterAttendeeCommand(
    TicketedEventId EventId,
    string FirstName,
    string LastName,
    EmailAddress EmailAddress,
    TicketRequest[] TicketRequests) : Command<RegistrationId>;
    
