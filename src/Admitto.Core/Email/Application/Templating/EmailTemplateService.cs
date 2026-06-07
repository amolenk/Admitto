using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Templating;

internal sealed class EmailTemplateService(IEmailWriteStore writeStore) : IEmailTemplateService
{
    public async ValueTask<EmailTemplate> LoadAsync(
        string name,
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        // Load all matching templates in one query, then pick by precedence.
        var candidates = await writeStore.EmailTemplates
            .AsNoTracking()
            .Where(t => t.Name == name &&
                        t.TeamId == teamId &&
                        (t.TicketedEventId == eventId || t.TicketedEventId == null))
            .ToListAsync(cancellationToken);

        var template = candidates.FirstOrDefault(t => t.TicketedEventId == eventId)
                    ?? candidates.FirstOrDefault(t => t.TicketedEventId == null);

        if (template is not null)
            return template;

        return BuildFromCatalog(name);
    }

    public async ValueTask<EmailTemplate> LoadAsync(
        string name,
        TeamId teamId,
        CancellationToken cancellationToken = default)
    {
        var template = await writeStore.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Name == name &&
                     t.TeamId == teamId &&
                     t.TicketedEventId == null,
                cancellationToken);

        if (template is not null)
            return template;

        return BuildFromCatalog(name);
    }

    private static EmailTemplate BuildFromCatalog(string name)
    {
        var entry = BuiltInEmailTemplateCatalog.GetByName(name)
            ?? throw new InvalidOperationException($"No template found for name '{name}' and no built-in default exists.");

        return EmailTemplate.Create(
            TeamId.New(),
            null,
            entry.Name,
            entry.DefaultSubject,
            entry.DefaultTextBody,
            entry.DefaultHtmlBody);
    }
}
