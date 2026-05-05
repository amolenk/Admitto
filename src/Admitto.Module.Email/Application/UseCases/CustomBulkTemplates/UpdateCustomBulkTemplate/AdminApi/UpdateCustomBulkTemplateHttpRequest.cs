namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.UpdateCustomBulkTemplate.AdminApi;

public sealed record UpdateCustomBulkTemplateHttpRequest(
    string Name,
    string Subject,
    string TextBody,
    string? HtmlBody,
    uint Version);
