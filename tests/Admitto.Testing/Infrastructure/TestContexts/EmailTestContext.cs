using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Testing.Infrastructure.TestContexts;

public class EmailTestContext
{
    public HttpClient Client { get; }
    public Uri SmtpEndpoint { get; }

    private EmailTestContext(HttpClient client, Uri smtpEndpoint)
    {
        Client = client;
        SmtpEndpoint = smtpEndpoint;
    }

    public static async ValueTask<EmailTestContext> CreateAsync(DistributedApplication appHost)
    {
        var clientFactory = appHost.Services.GetRequiredService<IHttpClientFactory>();
        var client = clientFactory.CreateClient("MailDev");

        var smtpEndpoint = appHost.GetEndpoint("maildev", "smtp");

        return new EmailTestContext(client, smtpEndpoint);
    }

    public async Task ResetAsync()
    {
        await Client.DeleteAsync("/api/email/all");
    }

    public async Task<List<JsonElement>> WaitForAsync(
        int expectedCount,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (true)
        {
            var mailDevResponse = await Client.GetAsync(
                "/api/email",
                cancellationToken);

            if (mailDevResponse.IsSuccessStatusCode)
            {
                var json = await mailDevResponse.Content.ReadFromJsonAsync<JsonElement>(
                    cancellationToken: cancellationToken);

                var emails = json.EnumerateArray().ToList();
                if (emails.Count >= expectedCount)
                    return emails;
            }

            if (DateTimeOffset.UtcNow >= deadline)
                return [];

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }

    /// <summary>
    /// Returns the lowercase recipient addresses (first <c>to</c> entry) from
    /// each captured MailDev message.
    /// </summary>
    public static IReadOnlyList<string> GetLowercaseRecipientAddresses(IEnumerable<JsonElement> messages)
    {
        var result = new List<string>();
        foreach (var msg in messages)
        {
            if (msg.TryGetProperty("to", out var to) && to.GetArrayLength() > 0)
            {
                var address = to[0].GetProperty("address").GetString();
                if (address is not null)
                    result.Add(address.ToLowerInvariant());
            }
        }
        return result;
    }
}
