using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeCouponEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.ComposeCouponEmail;

internal sealed class CouponEmailComposerFixture
{
    private CouponEmailComposerFixture() { }

    public static CouponEmailComposerFixture CompleteEventContext() => new();

    public async ValueTask SetupAsync(
        IntegrationTestEnvironment environment,
        TeamId teamId,
        TicketedEventId eventId)
    {
        var context = EventEmailContextView.CreatePartial(
            teamId,
            eventId,
            DateTimeOffset.UtcNow);
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

        await environment.EmailDatabase.SeedAsync(db => db.EventEmailContexts.Add(context));
        await environment.EmailDatabase.SeedAsync(db => db.TeamEmailContexts.Add(
            TeamEmailContextView.Create(
                teamId,
                "DevConf Team",
                "#0f766e",
                teamVersion: 1,
                DateTimeOffset.UtcNow)));
    }

    public CouponEmailComposer BuildComposer(IntegrationTestEnvironment environment)
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

        return new CouponEmailComposer(
            environment.EmailDatabase.Context,
            contextProvider,
            preparation,
            prepareDelivery);
    }
}
