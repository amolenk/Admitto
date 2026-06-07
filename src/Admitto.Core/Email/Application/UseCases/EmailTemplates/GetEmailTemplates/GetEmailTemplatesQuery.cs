using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplates;

internal sealed record GetEmailTemplatesQuery(
    TeamId TeamId,
    TicketedEventId? TicketedEventId) : Query<IReadOnlyList<EmailTemplateListItemDto>>;
