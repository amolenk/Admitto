using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.GetEventEmailTemplates;

public record GetEventEmailTemplatesResponse(EventEmailTemplateDto[] EmailTemplates);

public record EventEmailTemplateDto(
    EmailType Type,
    Guid? TicketedEventId);