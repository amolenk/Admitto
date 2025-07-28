using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.GetTeamEmailTemplates;

public record GetTeamEmailTemplatesResponse(TeamEmailTemplateDto[] EmailTemplates);

public record TeamEmailTemplateDto(EmailType Type);
