using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Composing;

[TestClass]
public sealed class RegistrationEmailLinksTests
{
    // Given a public event link and a registration
    // When registration email links are composed
    // Then every registration route uses the registration identifier
    [TestMethod]
    public void From_WithRegistration_ComposesRegistrationRoutes()
    {
        var registrationId = RegistrationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var links = RegistrationEmailLinks.From("https://tickets.example.com/e/devconf", registrationId);

        links.PublicEventLink.ShouldBe("https://tickets.example.com/e/devconf");
        links.RegisterLink.ShouldBe("https://tickets.example.com/e/devconf/register");
        links.QRCodeLink.ShouldBe("https://tickets.example.com/e/devconf/qr-code/11111111-1111-1111-1111-111111111111");
        links.CancelLink.ShouldBe("https://tickets.example.com/e/devconf/cancel/11111111-1111-1111-1111-111111111111");
        links.EditRegistrationLink.ShouldBe("https://tickets.example.com/e/devconf/edit/11111111-1111-1111-1111-111111111111");
        links.ReconfirmLink.ShouldBe("https://tickets.example.com/e/devconf/reconfirm/11111111-1111-1111-1111-111111111111");
    }

    // Given only a public event link
    // When registration email links are composed without a registration
    // Then registration-specific routes fall back to the public event link
    [TestMethod]
    public void From_WithoutRegistration_UsesPublicEventLinkFallbacks()
    {
        var links = RegistrationEmailLinks.From("https://tickets.example.com/e/devconf", null);

        links.PublicEventLink.ShouldBe("https://tickets.example.com/e/devconf");
        links.RegisterLink.ShouldBe("https://tickets.example.com/e/devconf/register");
        links.QRCodeLink.ShouldBe(links.PublicEventLink);
        links.CancelLink.ShouldBe(links.PublicEventLink);
        links.EditRegistrationLink.ShouldBe(links.PublicEventLink);
        links.ReconfirmLink.ShouldBe(links.PublicEventLink);
    }
}
