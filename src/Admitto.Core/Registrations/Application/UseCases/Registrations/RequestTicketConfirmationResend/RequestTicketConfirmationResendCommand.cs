using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RequestTicketConfirmationResend;

internal sealed record RequestTicketConfirmationResendCommand(
    Guid TeamId,
    Guid TicketedEventId,
    Guid RegistrationId,
    Guid ResendRequestId) : Command;
