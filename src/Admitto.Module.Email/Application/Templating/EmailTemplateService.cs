using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Email.Application.Templating;

internal sealed class EmailTemplateService(IEmailWriteStore writeStore) : IEmailTemplateService
{
    public async ValueTask<EmailTemplate> LoadAsync(
        string type,
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        // Load all matching templates in one query, then pick by precedence.
        var candidates = await writeStore.EmailTemplates
            .AsNoTracking()
            .Where(t => t.Type == type &&
                        ((t.Scope == EmailSettingsScope.Event && t.ScopeId == eventId.Value) ||
                         (t.Scope == EmailSettingsScope.Team  && t.ScopeId == teamId.Value)))
            .ToListAsync(cancellationToken);

        var template = candidates.FirstOrDefault(t => t.Scope == EmailSettingsScope.Event)
                    ?? candidates.FirstOrDefault(t => t.Scope == EmailSettingsScope.Team);

        if (template is not null)
            return template;

        return DefaultEmailTemplates.Get(type)
               ?? throw new InvalidOperationException($"No template found for type '{type}' and no built-in default exists.");
    }
}
