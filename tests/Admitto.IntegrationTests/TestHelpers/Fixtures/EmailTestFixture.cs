using Amolenk.Admitto.Domain.ValueObjects;
using Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers.Fixtures;

public class EmailTestFixture
{
    private readonly HttpClient _client;
    
    private EmailTestFixture(HttpClient client, EmailSettings defaultEmailSettings)
    {
        _client = client;

        DefaultEmailSettings = defaultEmailSettings;
    }
 
    public EmailSettings DefaultEmailSettings { get; }

    public static EmailTestFixture Create(TestingAspireAppHost appHost)
    {
        var client = appHost.Application.CreateHttpClient("maildev", "http");
        
        var smtpEndpoint = appHost.Application.GetEndpoint("maildev", "smtp");
        var defaultEmailSettings = new EmailSettingsBuilder()
            .WithSmtpServer(smtpEndpoint.Host)
            .WithPort(smtpEndpoint.Port)
            .Build();
        
        return new EmailTestFixture(client, defaultEmailSettings);
    }
    
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync("/email/all", cancellationToken);
    }
    
    public async Task<IEnumerable<SentEmail>> GetSentEmailsAsync(CancellationToken cancellationToken = default)
    {
        var emails = await _client.GetFromJsonAsync<List<SentEmail>>("/email", cancellationToken);
        return emails ?? [];
    }
}

public class SentEmail
{
    public required string Html { get; set; }
    public required string Subject { get; set; }
    public required List<EmailAddress> From { get; set; }
    public required List<EmailAddress> To { get; set; }
}

public class EmailAddress
{
    public required string Address { get; set; }
    public required string Name { get; set; }
}
