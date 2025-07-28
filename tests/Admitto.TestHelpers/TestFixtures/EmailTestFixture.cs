using Amolenk.Admitto.Domain.ValueObjects;
using Aspire.Hosting.Testing;

namespace Amolenk.Admitto.TestHelpers.TestFixtures;

public class EmailTestFixture
{
    private EmailTestFixture(EmailSettings defaultEmailSettings)
    {
        DefaultEmailSettings = defaultEmailSettings;
    }
 
    public EmailSettings DefaultEmailSettings { get; }

    public static EmailTestFixture Create(TestingAspireAppHost appHost)
    {
        var defaultEmailSettings = new EmailSettings(
            "test@example.com",
            appHost.Application.GetEndpoint("maildev", "http").Host,
            appHost.Application.GetEndpoint("maildev", "http").Port);
        
        return new EmailTestFixture(defaultEmailSettings);
    }
}