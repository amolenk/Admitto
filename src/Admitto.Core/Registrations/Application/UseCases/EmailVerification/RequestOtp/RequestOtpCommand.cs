using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.RequestOtp;

internal sealed record RequestOtpCommand(
    TeamId TeamId,
    TicketedEventId EventId,
    string Email) : Command;

