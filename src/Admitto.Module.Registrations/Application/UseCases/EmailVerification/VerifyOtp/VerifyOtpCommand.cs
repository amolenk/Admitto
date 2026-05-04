using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EmailVerification.VerifyOtp;

internal sealed record VerifyOtpCommand(
    TeamId TeamId,
    TicketedEventId EventId,
    string Email,
    string Code) : Command<string>;
