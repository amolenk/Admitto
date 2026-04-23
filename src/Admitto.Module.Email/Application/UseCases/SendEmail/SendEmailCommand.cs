using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.SendEmail;

public sealed record SendEmailCommand(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string RecipientAddress,
    string RecipientName,
    string EmailType,
    string IdempotencyKey,
    object Parameters) : Command;
