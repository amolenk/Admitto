using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.RequestOtp;

internal sealed record RequestOtpCommand(
    TeamId TeamId,
    TicketedEventId EventId,
    string Email) : Command;

