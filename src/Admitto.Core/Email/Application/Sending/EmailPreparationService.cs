using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Sending;

internal interface IEmailPreparationService
{
    ValueTask<RenderedEmail> PrepareAsync(
        string emailType,
        TeamId teamId,
        TicketedEventId eventId,
        object parameters,
        CancellationToken cancellationToken = default);
}

internal sealed class EmailPreparationService(
    IEmailReadStore readStore,
    IEmailTemplateService templateService,
    IEmailRenderer renderer) : IEmailPreparationService
{
    public async ValueTask<RenderedEmail> PrepareAsync(
        string emailType,
        TeamId teamId,
        TicketedEventId eventId,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        // Branding is Email-owned context. SMTP transport is deployment-global and is
        // deliberately not resolved while preparing a message.
        var accentColor = await readStore.TeamEmailContexts
                .AsNoTracking()
                .Where(c => c.TeamId == teamId)
                .Select(c => (AccentColor?)c.AccentColor)
                .SingleOrDefaultAsync(cancellationToken)
            ?? AccentColor.From(AccentColor.Default);
        var fontFamily = EmailFontFamily.From(EmailFontFamily.Default);

        var template = await templateService.LoadAsync(
            emailType,
            teamId,
            eventId,
            cancellationToken);
        return renderer.Render(
            template,
            EmailTemplateParameters.WithBranding(
                parameters,
                accentColor,
                fontFamily));

    }
}
