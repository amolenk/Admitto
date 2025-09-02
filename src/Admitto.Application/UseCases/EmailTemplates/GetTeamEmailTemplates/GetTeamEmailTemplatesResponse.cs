namespace Amolenk.Admitto.Application.UseCases.EmailTemplates.GetTeamEmailTemplates;

public record GetTeamEmailTemplatesResponse(TeamEmailTemplateDto[] EmailTemplates);

public record TeamEmailTemplateDto(string Type, bool IsCustom);
