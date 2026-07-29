using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ReconfirmRegistration;

internal sealed record ReconfirmRegistrationCommand(
    Guid RegistrationId,
    Guid TicketedEventId,
    Guid TeamId) : Command;
