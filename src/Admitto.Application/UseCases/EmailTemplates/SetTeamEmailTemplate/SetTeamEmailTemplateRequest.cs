namespace Amolenk.Admitto.Application.UseCases.EmailTemplates.SetTeamEmailTemplate;

public record SetTeamEmailTemplateRequest(string Subject, string TextBody, string HtmlBody);