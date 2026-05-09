namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate.AdminApi;

public sealed record CreateEmailTemplateHttpRequest(
    string Name,
    string? Subject,
    string? TextBody,
    string? HtmlBody);
