using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Sending;

[TestClass]
public sealed class EmailPreparationServiceTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a team whose branding projection has not arrived
    // When an email is prepared
    // Then the default accent color is applied to the rendered content
    [TestMethod]
    public async ValueTask PrepareAsync_MissingTeamBranding_UsesDefaultAccentColor()
    {
        var fixture = EmailPreparationServiceFixture.DefaultBrandingFallback();
        var rendered = await fixture.BuildService(Environment).PrepareAsync(
            BuiltInEmailTemplateNames.TicketConfirmation,
            TeamId.New(),
            TicketedEventId.New(),
            new
            {
                FirstName = "Alice",
                LastName = "Anderson",
                TeamName = "Admitto",
                EventName = "DevConf",
                EventWebsite = "https://devconf.example.com",
                PublicEventLink = "https://admitto.example.com/e/devconf",
                QRCodeLink = "https://admitto.example.com/e/devconf/qr-code/registration",
                CancelLink = "https://admitto.example.com/e/devconf/cancel/registration",
                EditRegistrationLink = "https://admitto.example.com/e/devconf/edit/registration",
                TicketTypes = Array.Empty<string>()
            },
            testContext.CancellationToken);

        rendered.HtmlBody.ShouldContain(AccentColor.Default);
    }

    // Given ticket-confirmation parameters missing a required template variable
    // When the email is prepared
    // Then strict Scriban rendering throws an EmailRenderException
    [TestMethod]
    public async ValueTask PrepareAsync_MissingTemplateVariable_ThrowsEmailRenderException()
    {
        var fixture = EmailPreparationServiceFixture.DefaultBrandingFallback();

        await Should.ThrowAsync<EmailRenderException>(async () =>
            await fixture.BuildService(Environment).PrepareAsync(
                BuiltInEmailTemplateNames.TicketConfirmation,
                TeamId.New(),
                TicketedEventId.New(),
                new { FirstName = "Alice" },
                testContext.CancellationToken));
    }
}
