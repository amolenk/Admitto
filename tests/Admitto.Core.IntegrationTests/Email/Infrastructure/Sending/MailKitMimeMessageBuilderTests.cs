using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Email.Infrastructure.Sending;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using MimeKit;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Infrastructure.Sending;

[TestClass]
public sealed class MailKitMimeMessageBuilderTests
{
    [TestMethod]
    public void Build_UsesConfiguredSystemSenderAndNeverSetsReplyTo()
    {
        var settings = CreateSettings("Admitto");

        var result = MailKitMimeMessageBuilder.Build(settings, CreateMessage());

        var from = result.From.Mailboxes.Single();
        from.Name.ShouldBe("Admitto");
        from.Address.ShouldBe("tickets@admitto.org");

        // Sending on behalf of a team hurts deliverability, so no Reply-To is ever set.
        result.ReplyTo.ShouldBeEmpty();
    }

    private static EffectiveEmailSettings CreateSettings(string fromDisplayName) =>
        new(
            Hostname.From("smtp.admitto.org"),
            Port.From(587),
            SmtpSsl: false,
            SmtpStartTls: true,
            EmailAddress.From("tickets@admitto.org"),
            fromDisplayName,
            EmailAuthMode.None,
            Username: null,
            Password: null,
            AccentColor.From("#0f766e"),
            EmailFontFamily.From("Arial"));

    private static EmailMessage CreateMessage() =>
        new(
            "alice@example.com",
            "Alice Adams",
            "Your ticket",
            "Plain text body",
            "<p>HTML body</p>");
}
