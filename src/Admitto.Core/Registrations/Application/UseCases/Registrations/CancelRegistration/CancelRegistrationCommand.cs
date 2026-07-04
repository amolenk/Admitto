using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelRegistration;

internal sealed record CancelRegistrationCommand(
    Guid RegistrationId,
    Guid TicketedEventId,
    Guid TeamId,
    CancellationReason Reason) : Command;
