using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeReconfirmation;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Sending;

internal interface IEmailPreparationService
{
    ValueTask<ReconfirmationEmailCompositionScope> CreateReconfirmationScopeAsync(
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default);

    ValueTask<RenderedEmail> PrepareAsync(
        string emailType,
        TeamId teamId,
        TicketedEventId eventId,
        object parameters,
        CancellationToken cancellationToken = default);

    ValueTask<RenderedEmail> PrepareAsync(
        string emailType,
        TeamId teamId,
        TicketedEventId eventId,
        object parameters,
        AccentColor accentColor,
        CancellationToken cancellationToken = default);
}

internal sealed class EmailPreparationService(
    IEmailReadStore readStore,
    IEmailTemplateService templateService,
    IEmailRenderer renderer,
    IEventEmailRenderingContextProvider eventContextProvider) : IEmailPreparationService
{
    public async ValueTask<ReconfirmationEmailCompositionScope> CreateReconfirmationScopeAsync(
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        var eventContext = await eventContextProvider.GetScopeAsync(
            teamId,
            eventId,
            cancellationToken);
        var template = await templateService.LoadAsync(
            BuiltInEmailTemplateNames.Reconfirmation,
            teamId,
            eventId,
            cancellationToken);

        return new ReconfirmationEmailCompositionScope(
            eventContext,
            template,
            EmailFontFamily.From(EmailFontFamily.Default),
            renderer);
    }

    public async ValueTask<RenderedEmail> PrepareAsync(
        string emailType,
        TeamId teamId,
        TicketedEventId eventId,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        var accentColor = await readStore.TeamEmailContexts
                .AsNoTracking()
                .Where(c => c.TeamId == teamId)
                .Select(c => (AccentColor?)c.AccentColor)
                .SingleOrDefaultAsync(cancellationToken)
            ?? AccentColor.From(AccentColor.Default);
        return await PrepareAsync(emailType, teamId, eventId, parameters, accentColor, cancellationToken);
    }

    public async ValueTask<RenderedEmail> PrepareAsync(
        string emailType,
        TeamId teamId,
        TicketedEventId eventId,
        object parameters,
        AccentColor accentColor,
        CancellationToken cancellationToken = default)
    {
        // Branding is Email-owned context. SMTP transport is deployment-global and is
        // deliberately not resolved while preparing a message.
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
