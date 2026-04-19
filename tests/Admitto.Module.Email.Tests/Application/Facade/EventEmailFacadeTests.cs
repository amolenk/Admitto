using Amolenk.Admitto.Module.Email.Application.UseCases;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Email.Tests.Application.UseCases.EventEmailSettings;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Tests.Application.Facade;

[TestClass]
public sealed class EventEmailFacadeTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask IsEmailConfigured_ConfiguredRow_ReturnsTrue()
    {
        var (eventId, _) = await EventEmailSettingsFixture.SeedBasicAsync(Environment);

        var facade = new EventEmailFacade(Environment.Database.Context);

        var result = await facade.IsEmailConfiguredAsync(eventId, testContext.CancellationToken);

        result.ShouldBeTrue();
    }

    [TestMethod]
    public async ValueTask IsEmailConfigured_MissingRow_ReturnsFalse()
    {
        var facade = new EventEmailFacade(Environment.Database.Context);

        var result = await facade.IsEmailConfiguredAsync(TicketedEventId.New(), testContext.CancellationToken);

        result.ShouldBeFalse();
    }
}
