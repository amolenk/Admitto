using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Domain.Tests.Entities;

[TestClass]
public sealed class EventEmailSettingsTests
{
    [TestMethod]
    public void Create_WithValidNoneAuth_SetsFields()
    {
        var settings = new EventEmailSettingsBuilder().Build();

        settings.SmtpHost.Value.ShouldBe(EventEmailSettingsBuilder.DefaultSmtpHost);
        settings.SmtpPort.Value.ShouldBe(EventEmailSettingsBuilder.DefaultSmtpPort);
        settings.AuthMode.ShouldBe(EmailAuthMode.None);
        settings.Username.ShouldBeNull();
        settings.ProtectedPassword.ShouldBeNull();
    }

    [TestMethod]
    public void Create_WithBasicAuth_StoresCredentials()
    {
        var settings = new EventEmailSettingsBuilder().WithBasicAuth("alice", "ENCRYPTED:pw").Build();

        settings.AuthMode.ShouldBe(EmailAuthMode.Basic);
        settings.Username!.Value.Value.ShouldBe("alice");
        settings.ProtectedPassword!.Value.Ciphertext.ShouldBe("ENCRYPTED:pw");
    }

    [TestMethod]
    public void Create_WithBasicAuthMissingPassword_ThrowsBasicAuthRequiresCredentials()
    {
        var ex = Should.Throw<BusinessRuleViolationException>(() =>
            EventEmailSettings.Create(
                TicketedEventId.New(),
                Hostname.From("smtp.example.com"),
                Port.From(587),
                EmailAddress.From("noreply@example.com"),
                EmailAuthMode.Basic,
                username: SmtpUsername.From("alice"),
                protectedPassword: null));

        ex.Error.ShouldMatch(EventEmailSettings.Errors.BasicAuthRequiresCredentials);
    }

    [TestMethod]
    public void Update_WithNewPort_ChangesPortAndLeavesOtherFields()
    {
        var settings = new EventEmailSettingsBuilder().WithBasicAuth().Build();
        var originalHost = settings.SmtpHost;
        var originalProtectedPassword = settings.ProtectedPassword;

        settings.Update(smtpHost: null, smtpPort: Port.From(2525), fromAddress: null, authMode: null, username: null, protectedPassword: null);

        settings.SmtpHost.ShouldBe(originalHost);
        settings.SmtpPort.Value.ShouldBe(2525);
        settings.ProtectedPassword.ShouldBe(originalProtectedPassword);
    }

    [TestMethod]
    public void Update_WithNullPassword_PreservesStoredPassword()
    {
        var settings = new EventEmailSettingsBuilder().WithBasicAuth(protectedPassword: "ENCRYPTED:old").Build();

        settings.Update(null, null, null, null, username: SmtpUsername.From("bob"), protectedPassword: null);

        settings.Username!.Value.Value.ShouldBe("bob");
        settings.ProtectedPassword!.Value.Ciphertext.ShouldBe("ENCRYPTED:old");
    }

    [TestMethod]
    public void Update_SwitchingToNone_ClearsCredentials()
    {
        var settings = new EventEmailSettingsBuilder().WithBasicAuth().Build();

        settings.Update(null, null, null, EmailAuthMode.None, null, null);

        settings.AuthMode.ShouldBe(EmailAuthMode.None);
        settings.Username.ShouldBeNull();
        settings.ProtectedPassword.ShouldBeNull();
    }

    [TestMethod]
    public void IsValid_WithCompleteNoneAuth_ReturnsTrue()
    {
        var settings = new EventEmailSettingsBuilder().Build();

        settings.IsValid().ShouldBeTrue();
    }

    [TestMethod]
    public void IsValid_WithCompleteBasicAuth_ReturnsTrue()
    {
        var settings = new EventEmailSettingsBuilder().WithBasicAuth().Build();

        settings.IsValid().ShouldBeTrue();
    }
}
