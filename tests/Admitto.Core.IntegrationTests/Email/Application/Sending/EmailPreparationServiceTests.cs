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
            new { FirstName = "Alice", EventName = "DevConf" },
            testContext.CancellationToken);

        rendered.HtmlBody.ShouldContain(AccentColor.Default);
    }
}
