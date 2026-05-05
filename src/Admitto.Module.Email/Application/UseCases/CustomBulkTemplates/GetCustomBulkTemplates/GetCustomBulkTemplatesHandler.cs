using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.GetCustomBulkTemplates;

internal sealed class GetCustomBulkTemplatesHandler(IEmailWriteStore writeStore)
    : IQueryHandler<GetCustomBulkTemplatesQuery, IReadOnlyList<CustomBulkTemplateListItemDto>>
{
    public async ValueTask<IReadOnlyList<CustomBulkTemplateListItemDto>> HandleAsync(
        GetCustomBulkTemplatesQuery query,
        CancellationToken ct)
    {
        var templates = await writeStore.EmailTemplates
            .AsNoTracking()
            .Where(t => t.Scope == query.Scope && t.ScopeId == query.ScopeId && t.Type == EmailTemplateType.BulkCustom)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return templates
            .Select(t => new CustomBulkTemplateListItemDto(
                t.Id.Value,
                t.Name!,
                t.Subject,
                t.CreatedAt,
                t.LastChangedAt,
                t.Version))
            .ToList();
    }
}
