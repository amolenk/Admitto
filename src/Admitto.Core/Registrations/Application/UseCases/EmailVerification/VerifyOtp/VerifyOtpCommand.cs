using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.VerifyOtp;

internal sealed record VerifyOtpCommand(
    TeamId TeamId,
    TicketedEventId EventId,
    string Email,
    string Code) : Command<string>;
