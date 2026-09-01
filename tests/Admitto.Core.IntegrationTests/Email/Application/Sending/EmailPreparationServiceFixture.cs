using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Sending;

internal sealed class EmailPreparationServiceFixture
{
    private EmailPreparationServiceFixture() { }

    public static EmailPreparationServiceFixture DefaultBrandingFallback() => new();

    public EmailPreparationService BuildService(IntegrationTestEnvironment environment) =>
        new(
            environment.EmailDatabase.Context,
            new EmailTemplateService(),
            new ScribanEmailRenderer(),
            new EventEmailRenderingContextProvider(
                environment.EmailDatabase.Context,
                Options.Create(new PublicEventLinksOptions { BaseUrl = "https://public.example/e" })));
}
