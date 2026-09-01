using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeRegistrationCancellation;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.ComposeRegistrationCancellation;

internal sealed class RegistrationCancellationEmailComposerFixture
{
    private readonly bool _completeContext;
    private readonly string? _teamName;

    private RegistrationCancellationEmailComposerFixture(bool completeContext, string? teamName)
    {
        _completeContext = completeContext;
        _teamName = teamName;
    }

    public static RegistrationCancellationEmailComposerFixture CompleteEventContext() => new(true, null);

    public static RegistrationCancellationEmailComposerFixture ExistingTeamContext() =>
        new(true, "DevConf Team");

    public static RegistrationCancellationEmailComposerFixture IncompleteEventContext() => new(false, null);

    public async ValueTask SetupAsync(
        IntegrationTestEnvironment environment,
        TeamId teamId,
        TicketedEventId eventId)
    {
        var context = EventEmailContextView.CreatePartial(teamId, eventId, DateTimeOffset.UtcNow);

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

        if (_teamName is not null)
        {
            await environment.EmailDatabase.SeedAsync(db => db.TeamEmailContexts.Add(
                TeamEmailContextView.Create(
                    teamId,
                    _teamName,
                    "#0f766e",
                    teamVersion: 1,
                    DateTimeOffset.UtcNow)));
        }
    }

    public RegistrationCancellationEmailComposer BuildComposer(IntegrationTestEnvironment environment)
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

        return new RegistrationCancellationEmailComposer(
            environment.EmailDatabase.Context,
            contextProvider,
            preparation,
            prepareDelivery);
    }

    public async ValueTask SeedTerminalClaimAsync(
        IntegrationTestEnvironment environment,
        TeamId teamId,
        TicketedEventId eventId,
        RegistrationCancellationDelivery delivery,
        string emailType)
    {
        await environment.EmailDatabase.SeedAsync(db => db.EmailLog.Add(EmailLog.Create(
            teamId,
            eventId,
            delivery.IdempotencyKey,
            EmailAddress.From(delivery.RecipientAddress),
            emailType,
            "Your DevConf Registration Has Been Cancelled",
            EmailLogStatus.Sent,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            registrationId: delivery.RegistrationId)));
    }
}
