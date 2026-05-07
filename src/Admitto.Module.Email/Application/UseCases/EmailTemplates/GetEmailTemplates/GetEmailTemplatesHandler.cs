using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Application.Templating;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplates;

internal sealed class GetEmailTemplatesHandler(IEmailWriteStore writeStore)
    : IQueryHandler<GetEmailTemplatesQuery, IReadOnlyList<EmailTemplateListItemDto>>
{
    public async ValueTask<IReadOnlyList<EmailTemplateListItemDto>> HandleAsync(
        GetEmailTemplatesQuery query,
        CancellationToken ct)
    {
        var dbRows = await writeStore.EmailTemplates
            .AsNoTracking()
            .Where(t => t.Scope == query.Scope && t.ScopeId == query.ScopeId)
            .ToListAsync(ct);

        var result = new List<EmailTemplateListItemDto>();

        // Built-in templates first (ordered by catalog definition).
        foreach (var entry in BuiltInEmailTemplateCatalog.All)
        {
            var row = dbRows.FirstOrDefault(
                t => string.Equals(t.Name, entry.Name, StringComparison.OrdinalIgnoreCase));

            if (row is not null)
            {
                result.Add(new EmailTemplateListItemDto(
                    Id: row.Id.Value,
                    Name: row.Name,
                    Kind: "builtin",
                    Description: entry.Description,
                    Subject: row.Subject,
                    IsCustomised: true,
                    Version: row.Version));
            }
            else
            {
                result.Add(new EmailTemplateListItemDto(
                    Id: null,
                    Name: entry.Name,
                    Kind: "builtin",
                    Description: entry.Description,
                    Subject: entry.DefaultSubject,
                    IsCustomised: false,
                    Version: null));
            }
        }

        // Custom templates (rows not matching any built-in name).
        foreach (var row in dbRows.Where(t => !BuiltInEmailTemplateNames.IsReserved(t.Name)))
        {
            result.Add(new EmailTemplateListItemDto(
                Id: row.Id.Value,
                Name: row.Name,
                Kind: "custom",
                Description: null,
                Subject: row.Subject,
                IsCustomised: false,
                Version: row.Version));
        }

        // Custom templates from parent scope (e.g. team-level when listing for an event)
        // that have not already been overridden at the current scope.
        if (query.ParentScopeId.HasValue)
        {
            var parentCustomRows = await writeStore.EmailTemplates
                .AsNoTracking()
                .Where(t => t.Scope == EmailSettingsScope.Team &&
                            t.ScopeId == query.ParentScopeId.Value &&
                            !BuiltInEmailTemplateNames.IsReserved(t.Name))
                .ToListAsync(ct);

            foreach (var parentRow in parentCustomRows)
            {
                var alreadyOverridden = dbRows.Any(
                    t => string.Equals(t.Name, parentRow.Name, StringComparison.OrdinalIgnoreCase));

                if (!alreadyOverridden)
                {
                    result.Add(new EmailTemplateListItemDto(
                        Id: null,
                        Name: parentRow.Name,
                        Kind: "custom",
                        Description: null,
                        Subject: parentRow.Subject,
                        IsCustomised: false,
                        Version: null));
                }
            }
        }

        return result;
    }
}
