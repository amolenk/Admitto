using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;

/// <summary>
/// Claims an already-rendered message and schedules its durable SMTP delivery.
/// </summary>
internal sealed record PrepareEmailDeliveryCommand(
    Guid TeamId,
    Guid TicketedEventId,
    string RecipientAddress,
    string RecipientName,
    string EmailType,
    string IdempotencyKey,
    string Subject,
    string TextBody,
    string HtmlBody,
    Guid? RegistrationId = null,
    Guid? RegistrationCycleId = null) : Command;
