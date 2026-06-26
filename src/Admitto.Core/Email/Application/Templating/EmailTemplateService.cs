using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Templating;

internal sealed class EmailTemplateService : IEmailTemplateService
{
    public ValueTask<EmailTemplate> LoadAsync(
        string name,
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(BuildFromCatalog(name));
    }

    public ValueTask<EmailTemplate> LoadAsync(
        string name,
        TeamId teamId,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(BuildFromCatalog(name));
    }

    private static EmailTemplate BuildFromCatalog(string name)
    {
        return BuiltInEmailTemplateCatalog.CreateTemplate(name);
    }
}
