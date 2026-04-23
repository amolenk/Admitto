using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplate;

internal sealed class GetEmailTemplateHandler(IEmailWriteStore writeStore)
    : IQueryHandler<GetEmailTemplateQuery, EmailTemplateDto?>
{
    public async ValueTask<EmailTemplateDto?> HandleAsync(GetEmailTemplateQuery query, CancellationToken ct)
    {
        var template = await writeStore.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Scope == query.Scope && t.ScopeId == query.ScopeId && t.Type == query.Type,
                ct);

        if (template is null)
            return null;

        return new EmailTemplateDto(template.Subject, template.TextBody, template.HtmlBody, template.Version);
    }
}
