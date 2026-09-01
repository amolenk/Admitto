using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeVerificationCode;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.ComposeVerificationCode;

internal sealed class VerificationCodeEmailComposerFixture
{
    private readonly bool _completeContext;

    private VerificationCodeEmailComposerFixture(bool completeContext)
    {
        _completeContext = completeContext;
    }

    public static VerificationCodeEmailComposerFixture CompleteEventContext() => new(true);

    public static VerificationCodeEmailComposerFixture IncompleteEventContext() => new(false);

    public async ValueTask SetupAsync(
        IntegrationTestEnvironment environment,
        TeamId teamId,
        TicketedEventId eventId)
    {
        var context = EventEmailContextView.CreatePartial(
            teamId,
            eventId,
            DateTimeOffset.UtcNow);

        if (_completeContext)
        {
            context.UpdateEventContext(
                ticketedEventVersion: 1,
                eventName: "DevConf",
                websiteUrl: "https://devconf.example.com",
                publicSlug: "devconf",
                timeZone: "UTC",
                selfServiceTicketTypeCount: 1,
                reconfirmPolicy: null,
                isArchived: false,
                DateTimeOffset.UtcNow);
        }
        else
        {
            context.UpdateDetails(
                ticketedEventVersion: 1,
                eventName: "DevConf",
                websiteUrl: "https://devconf.example.com",
                publicSlug: "devconf",
                timeZone: "UTC",
                DateTimeOffset.UtcNow);
        }

        await environment.EmailDatabase.SeedAsync(db => db.EventEmailContexts.Add(context));
    }

    public VerificationCodeEmailComposer BuildComposer(IntegrationTestEnvironment environment)
    {
        var contextProvider = new EventEmailRenderingContextProvider(
            environment.EmailDatabase.Context,
            Options.Create(new PublicEventLinksOptions { BaseUrl = "https://public.example/e" }));
        var preparation = new EmailPreparationService(
            environment.EmailDatabase.Context,
            new EmailTemplateService(),
            new ScribanEmailRenderer(),
            contextProvider);
        var prepareDelivery = new PrepareEmailDeliveryHandler(
            environment.EmailDatabase.Context,
            new Outbox(environment.EmailDatabase.Context));

        return new VerificationCodeEmailComposer(
            environment.EmailDatabase.Context,
            contextProvider,
            preparation,
            prepareDelivery);
    }
}
