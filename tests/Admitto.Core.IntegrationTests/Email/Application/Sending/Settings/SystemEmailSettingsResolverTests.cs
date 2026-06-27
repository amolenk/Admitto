using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Sending.Settings;

[TestClass]
public sealed class SystemEmailSettingsResolverTests
{
    [TestMethod]
    public void Resolve_ValidSystemConfiguration_ReturnsEffectiveSettings()
    {
        var resolver = new SystemEmailSettingsResolver(Options.Create(new SystemEmailOptions
        {
            SmtpHost = "smtp.admitto.org",
            SmtpPort = 587,
            FromAddress = "tickets@admitto.org",
            AuthMode = nameof(EmailAuthMode.Basic),
            Username = "smtp-user",
            Password = "smtp-password"
        }));

        var result = resolver.Resolve();

        result.ShouldNotBeNull();
        result.SmtpHost.Value.ShouldBe("smtp.admitto.org");
        result.SmtpPort.Value.ShouldBe(587);
        result.FromAddress.Value.ShouldBe("tickets@admitto.org");
        result.AuthMode.ShouldBe(EmailAuthMode.Basic);
        result.Username.ShouldBe("smtp-user");
        result.Password.ShouldBe("smtp-password");
    }

    [TestMethod]
    public void Resolve_MissingRequiredConfiguration_ReturnsNull()
    {
        var resolver = new SystemEmailSettingsResolver(Options.Create(new SystemEmailOptions()));

        var result = resolver.Resolve();

        result.ShouldBeNull();
    }
}
