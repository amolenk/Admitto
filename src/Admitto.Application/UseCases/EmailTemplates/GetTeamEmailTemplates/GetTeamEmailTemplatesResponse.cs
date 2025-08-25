using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.EmailTemplates.GetTeamEmailTemplates;

public record GetTeamEmailTemplatesResponse(TeamEmailTemplateDto[] EmailTemplates);

public record TeamEmailTemplateDto(EmailType Type);
