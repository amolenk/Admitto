using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;

namespace Amolenk.Admitto.Api.Tests.Infrastructure;

/// <summary>
/// Helpers for asserting against the AppHost's MailDev dummy SMTP container.
/// MailDev exposes a small JSON HTTP API: <c>GET /email</c> lists every received
/// message, <c>DELETE /email/all</c> clears the inbox.
/// </summary>
internal static class MailDevHelper
{
    public static async Task ClearAsync(this EndToEndTestEnvironment environment, CancellationToken ct)
    {
        await environment.MailDevClient.DeleteAsync("/email/all", ct);
    }

    /// <summary>
    /// Polls <c>GET /email</c> until at least <paramref name="expectedCount"/>
    /// messages have arrived or the timeout elapses. Returns whatever has been
    /// observed at the end of the wait (which may be less than expected).
    /// </summary>
    public static async Task<List<JsonElement>> PollAsync(
        this EndToEndTestEnvironment environment,
        int expectedCount,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        var lastObserved = new List<JsonElement>();

        while (true)
        {
            var response = await environment.MailDevClient.GetAsync("/email", ct);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                lastObserved = json.EnumerateArray().ToList();
                if (lastObserved.Count >= expectedCount)
                    return lastObserved;
            }

            if (DateTimeOffset.UtcNow >= deadline)
                return lastObserved;

            await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
        }
    }

    /// <summary>
    /// Returns the lowercase recipient addresses (first <c>to</c> entry) from
    /// each captured MailDev message.
    /// </summary>
    public static IReadOnlyList<string> RecipientAddresses(this IEnumerable<JsonElement> messages)
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
