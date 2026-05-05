namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.CreateCustomBulkTemplate.AdminApi;

public sealed record CreateCustomBulkTemplateHttpRequest(
    string Name,
    string Subject,
    string TextBody,
    string? HtmlBody);
