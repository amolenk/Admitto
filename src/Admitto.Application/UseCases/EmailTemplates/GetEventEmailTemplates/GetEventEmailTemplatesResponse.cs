namespace Amolenk.Admitto.Application.UseCases.EmailTemplates.GetEventEmailTemplates;

public record GetEventEmailTemplatesResponse(EventEmailTemplateDto[] EmailTemplates);

public record EventEmailTemplateDto(
    string Type,
    Guid? TicketedEventId,
    bool IsCustom);