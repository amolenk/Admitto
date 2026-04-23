using Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.CreateEventEmailSettings;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Should = Shouldly.Should;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Tests.Application.UseCases.EventEmailSettings.CreateEventEmailSettings;

[TestClass]
public sealed class CreateEventEmailSettingsTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask Create_NewEvent_PersistsEncryptedRow()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var protectedSecret = TestProtectedSecretFactory.Create();
        const string plaintextPassword = "super-secret-password";

        var command = new CreateEventEmailSettingsCommand(
            eventId,
            "smtp.example.com",
            587,
            "noreply@example.com",
            EmailAuthMode.Basic,
            "alice",
            plaintextPassword);

        var handler = new CreateEventEmailSettingsHandler(Environment.Database.Context, protectedSecret);

        // Act
        await handler.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async db =>
        {
            var stored = await db.EmailSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Scope == EmailSettingsScope.Event && s.ScopeId == eventId, testContext.CancellationToken);

            stored.ShouldNotBeNull();
            stored.ProtectedPassword.ShouldNotBeNull();
            stored.ProtectedPassword.Value.Ciphertext.ShouldNotContain(plaintextPassword);
            protectedSecret.Unprotect(stored.ProtectedPassword.Value.Ciphertext).ShouldBe(plaintextPassword);
        });
    }

    [TestMethod]
    public async ValueTask Create_DuplicateEvent_ThrowsOnSave()
    {
        // Arrange — seed an existing row for the event
        var (eventId, _) = await EventEmailSettingsFixture.SeedBasicAsync(Environment);
        var protectedSecret = TestProtectedSecretFactory.Create();

        var command = new CreateEventEmailSettingsCommand(
            eventId.Value,
            "smtp.other.example.com",
            25,
            "noreply@example.com",
            EmailAuthMode.None,
            null,
            null);

        var handler = new CreateEventEmailSettingsHandler(Environment.Database.Context, protectedSecret);

        // Act — handler succeeds, SaveChanges is what fails (PK conflict)
        await handler.HandleAsync(command, testContext.CancellationToken);

        var exception = await Should.ThrowAsync<DbUpdateException>(
            () => Environment.Database.Context.SaveChangesAsync(testContext.CancellationToken));

        var postgresException = exception.InnerException.ShouldBeOfType<PostgresException>();
        postgresException.ConstraintName.ShouldBe("IX_email_settings_scope_scope_id");
    }
}
