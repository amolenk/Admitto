using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.CancelRegistration;

internal sealed record CancelRegistrationCommand(
    RegistrationId RegistrationId,
    TicketedEventId TicketedEventId,
    CancellationReason Reason) : Command;
