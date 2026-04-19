using Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.GetEventEmailSettings;
using Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Email.Tests.Application.UseCases.EventEmailSettings;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Tests.Application.UseCases.EventEmailSettings.GetEventEmailSettings;

[TestClass]
public sealed class GetEventEmailSettingsTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask Get_Existing_ReturnsDtoWithoutPassword()
    {
        var (eventId, _) = await EventEmailSettingsFixture.SeedBasicAsync(Environment);

        var handler = new GetEventEmailSettingsHandler(Environment.Database.Context);

        var dto = await handler.HandleAsync(
            new GetEventEmailSettingsQuery(eventId.Value),
            testContext.CancellationToken);

        dto.ShouldNotBeNull();
        dto.HasPassword.ShouldBeTrue();
    }

    [TestMethod]
    public async ValueTask Get_Missing_ReturnsNull()
    {
        var handler = new GetEventEmailSettingsHandler(Environment.Database.Context);

        var dto = await handler.HandleAsync(
            new GetEventEmailSettingsQuery(Guid.NewGuid()),
            testContext.CancellationToken);

        dto.ShouldBeNull();
    }
}
