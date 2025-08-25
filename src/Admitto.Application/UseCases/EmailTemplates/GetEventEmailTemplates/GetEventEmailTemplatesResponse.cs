using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.EmailTemplates.GetEventEmailTemplates;

public record GetEventEmailTemplatesResponse(EventEmailTemplateDto[] EmailTemplates);

public record EventEmailTemplateDto(
    EmailType Type,
    Guid? TicketedEventId);