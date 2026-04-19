using Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.UpdateEventEmailSettings;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Should = Shouldly.Should;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Tests.Application.UseCases.EventEmailSettings.UpdateEventEmailSettings;

[TestClass]
public sealed class UpdateEventEmailSettingsTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask Update_WithCorrectVersion_AppliesChanges()
    {
        var protectedSecret = TestProtectedSecretFactory.Create();
        var seedPassword = protectedSecret.Protect("initial-password");
        var (eventId, version) = await EventEmailSettingsFixture.SeedBasicAsync(
            Environment, protectedPassword: seedPassword);

        var command = new UpdateEventEmailSettingsCommand(
            eventId.Value,
            SmtpHost: "smtp.new.example.com",
            SmtpPort: 2525,
            FromAddress: null,
            AuthMode: null,
            Username: null,
            Password: null,
            ExpectedVersion: version);

        var handler = new UpdateEventEmailSettingsHandler(Environment.Database.Context, protectedSecret);

        await handler.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async db =>
        {
            Environment.Database.Context.ChangeTracker.Clear();
            var stored = await db.EventEmailSettings.AsNoTracking()
                .FirstAsync(s => s.Id == eventId, testContext.CancellationToken);
            stored.SmtpHost.Value.ShouldBe("smtp.new.example.com");
            stored.SmtpPort.Value.ShouldBe(2525);
            // Password preserved
            stored.ProtectedPassword!.Value.Ciphertext.ShouldBe(seedPassword);
        });
    }

    [TestMethod]
    public async ValueTask Update_WithStaleVersion_ThrowsConcurrency()
    {
        var protectedSecret = TestProtectedSecretFactory.Create();
        var (eventId, version) = await EventEmailSettingsFixture.SeedBasicAsync(Environment);

        var command = new UpdateEventEmailSettingsCommand(
            eventId.Value,
            SmtpHost: "smtp.changed.example.com",
            SmtpPort: null,
            FromAddress: null,
            AuthMode: null,
            Username: null,
            Password: null,
            ExpectedVersion: version + 1);

        var handler = new UpdateEventEmailSettingsHandler(Environment.Database.Context, protectedSecret);

        await Should.ThrowAsync<BusinessRuleViolationException>(
            () => handler.HandleAsync(command, testContext.CancellationToken).AsTask());
    }

    [TestMethod]
    public async ValueTask Update_NonExistingEvent_ThrowsNotFound()
    {
        var protectedSecret = TestProtectedSecretFactory.Create();
        var command = new UpdateEventEmailSettingsCommand(
            TicketedEventId.New().Value,
            "smtp.example.com", 587, "noreply@example.com", EmailAuthMode.None, null, null, ExpectedVersion: 1);

        var handler = new UpdateEventEmailSettingsHandler(Environment.Database.Context, protectedSecret);

        await Should.ThrowAsync<BusinessRuleViolationException>(
            () => handler.HandleAsync(command, testContext.CancellationToken).AsTask());
    }
}
