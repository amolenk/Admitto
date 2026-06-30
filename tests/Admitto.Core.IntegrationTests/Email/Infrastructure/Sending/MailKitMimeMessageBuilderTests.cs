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
    public void Build_ReplyToAddress_UsesReplyToAsFromDisplayNameAndKeepsHeaders()
    {
        var settings = CreateSettings(EmailAddress.From("help@example.com"));

        var result = MailKitMimeMessageBuilder.Build(settings, CreateMessage());

        var from = result.From.Mailboxes.Single();
        from.Name.ShouldBe("help@example.com");
        from.Address.ShouldBe("tickets@admitto.org");

        var replyTo = result.ReplyTo.Mailboxes.Single();
        replyTo.Name.ShouldBe("help@example.com");
        replyTo.Address.ShouldBe("help@example.com");
    }

    [TestMethod]
    public void Build_MissingReplyToAddress_UsesSystemFromAddressAsDisplayName()
    {
        var settings = CreateSettings(replyToAddress: null);

        var result = MailKitMimeMessageBuilder.Build(settings, CreateMessage());

        var from = result.From.Mailboxes.Single();
        from.Name.ShouldBe("tickets@admitto.org");
        from.Address.ShouldBe("tickets@admitto.org");
        result.ReplyTo.ShouldBeEmpty();
    }

    [TestMethod]
    public void Build_BulkSendPath_UsesReplyToAsFromDisplayNameAndKeepsHeaders()
    {
        var result = MailKitMimeMessageBuilder.Build(
            EmailAddress.From("tickets@admitto.org"),
            EmailAddress.From("help@example.com"),
            CreateMessage());

        var from = result.From.Mailboxes.Single();
        from.Name.ShouldBe("help@example.com");
        from.Address.ShouldBe("tickets@admitto.org");

        var replyTo = result.ReplyTo.Mailboxes.Single();
        replyTo.Name.ShouldBe("help@example.com");
        replyTo.Address.ShouldBe("help@example.com");
    }

    private static EffectiveEmailSettings CreateSettings(EmailAddress? replyToAddress) =>
        new(
            Hostname.From("smtp.admitto.org"),
            Port.From(587),
            SmtpSsl: false,
            SmtpStartTls: true,
            EmailAddress.From("tickets@admitto.org"),
            replyToAddress,
            EmailAuthMode.None,
            Username: null,
            Password: null,
            EmailAccentColor.From("#0f766e"),
            EmailFontFamily.From("Arial"));

    private static EmailMessage CreateMessage() =>
        new(
            "alice@example.com",
            "Alice Adams",
            "Your ticket",
            "Plain text body",
            "<p>HTML body</p>");
}
