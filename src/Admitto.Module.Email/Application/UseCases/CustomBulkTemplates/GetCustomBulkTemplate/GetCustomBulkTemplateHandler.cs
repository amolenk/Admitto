using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.GetCustomBulkTemplate;

internal sealed class GetCustomBulkTemplateHandler(IEmailWriteStore writeStore)
    : IQueryHandler<GetCustomBulkTemplateQuery, CustomBulkTemplateDto?>
{
    public async ValueTask<CustomBulkTemplateDto?> HandleAsync(
        GetCustomBulkTemplateQuery query,
        CancellationToken ct)
    {
        var template = await writeStore.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == query.Id && t.Type == EmailTemplateType.BulkCustom, ct);

        if (template is null)
            return null;

        return new CustomBulkTemplateDto(
            template.Id.Value,
            template.Name!,
            template.Subject,
            template.TextBody,
            template.HtmlBody,
            template.CreatedAt,
            template.LastChangedAt,
            template.Version);
    }
}
