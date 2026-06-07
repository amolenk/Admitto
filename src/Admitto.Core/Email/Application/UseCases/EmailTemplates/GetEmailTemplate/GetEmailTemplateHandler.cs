using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplate;

internal sealed class GetEmailTemplateHandler(IEmailWriteStore writeStore)
    : IQueryHandler<GetEmailTemplateQuery, EmailTemplateDto?>
{
    public async ValueTask<EmailTemplateDto?> HandleAsync(GetEmailTemplateQuery query, CancellationToken ct)
    {
        var template = await writeStore.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == query.Id && t.TeamId == query.TeamId && t.TicketedEventId == query.TicketedEventId,
                ct);

        if (template is null)
            return null;

        var catalogEntry = BuiltInEmailTemplateCatalog.GetByName(template.Name);
        var kind = catalogEntry is not null ? "builtin" : "custom";

        return new EmailTemplateDto(
            Id: template.Id.Value,
            Name: template.Name,
            Kind: kind,
            Description: catalogEntry?.Description,
            Subject: template.Subject,
            TextBody: template.TextBody,
            HtmlBody: template.HtmlBody,
            IsCustomised: catalogEntry is not null,
            Version: template.Version);
    }
}
