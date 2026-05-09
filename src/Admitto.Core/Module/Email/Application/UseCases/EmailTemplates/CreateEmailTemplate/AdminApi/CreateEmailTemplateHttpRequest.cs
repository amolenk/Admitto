namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate.AdminApi;

public sealed record CreateEmailTemplateHttpRequest(
    string Name,
    string? Subject,
    string? TextBody,
    string? HtmlBody);
