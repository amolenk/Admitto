using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Composing;

internal sealed class TransactionalEmailComposerFixture
{
    private readonly bool _completeContext;
    private readonly string? _teamName;

    private TransactionalEmailComposerFixture(bool completeContext, string? teamName)
    {
        _completeContext = completeContext;
        _teamName = teamName;
    }

    public static TransactionalEmailComposerFixture CompleteEventContext() => new(true, null);

    public static TransactionalEmailComposerFixture ExistingTeamContext() => new(true, "DevConf Team");

    public static TransactionalEmailComposerFixture IncompleteEventContext() => new(false, null);

    public async ValueTask SetupAsync(
        IntegrationTestEnvironment environment,
        TeamId teamId,
        TicketedEventId eventId)
    {
        var now = DateTimeOffset.UtcNow;
        var context = EventEmailContextView.CreatePartial(teamId, eventId, now);
        if (_completeContext)
        {
            context.UpdateEventContext(1, "DevConf", "https://devconf.example.com", "devconf", "UTC", 1,
                null, false, now);
        }
        else
        {
            context.UpdateDetails(1, "DevConf", "https://devconf.example.com", "devconf", "UTC", now);
        }

        await environment.EmailDatabase.SeedAsync(db => db.EventEmailContexts.Add(context));
        if (_teamName is not null)
        {
            await environment.EmailDatabase.SeedAsync(db => db.TeamEmailContexts.Add(
                TeamEmailContextView.Create(teamId, _teamName, "#0f766e", 1, now)));
        }
    }

    public TransactionalEmailComposer BuildComposer(IntegrationTestEnvironment environment) =>
        new(
            environment.EmailDatabase.Context,
            new ScribanEmailRenderer(),
            Options.Create(new PublicEventLinksOptions { BaseUrl = "https://public.example/e" }));

    public TransactionalEmailComposer BuildComposerWithDefaultPublicLink(IntegrationTestEnvironment environment) =>
        new(
            environment.EmailDatabase.Context,
            new ScribanEmailRenderer(),
            Options.Create(new PublicEventLinksOptions()));
}
