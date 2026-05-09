using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.Registrations.CancelRegistration;

internal sealed record CancelRegistrationCommand(
    Guid RegistrationId,
    Guid TicketedEventId,
    CancellationReason Reason) : Command;
