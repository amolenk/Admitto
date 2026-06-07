using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.GetEmailSettings;

internal sealed record GetEmailSettingsQuery(
    Guid TeamId,
    Guid? TicketedEventId) : Query<EmailSettingsDto?>;
