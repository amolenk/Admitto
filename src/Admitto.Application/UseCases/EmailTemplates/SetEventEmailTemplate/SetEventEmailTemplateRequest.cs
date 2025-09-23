namespace Amolenk.Admitto.Application.UseCases.EmailTemplates.SetEventEmailTemplate;

public record SetEventEmailTemplateRequest(string Subject, string TextBody, string HtmlBody);