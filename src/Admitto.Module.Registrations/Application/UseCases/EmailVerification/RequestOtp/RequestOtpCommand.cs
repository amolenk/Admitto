using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EmailVerification.RequestOtp;

internal sealed record RequestOtpCommand(
    TeamId TeamId,
    TicketedEventId EventId,
    string Email) : Command;

