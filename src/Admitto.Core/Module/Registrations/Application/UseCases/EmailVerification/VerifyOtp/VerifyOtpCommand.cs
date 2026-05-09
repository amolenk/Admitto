using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.EmailVerification.VerifyOtp;

internal sealed record VerifyOtpCommand(
    TeamId TeamId,
    TicketedEventId EventId,
    string Email,
    string Code) : Command<string>;
