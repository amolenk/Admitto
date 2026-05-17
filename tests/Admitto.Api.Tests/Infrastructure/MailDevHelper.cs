using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure.Hosting;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Api.Tests.Infrastructure;

/// <summary>
/// Helpers for asserting against the AppHost's MailDev dummy SMTP container.
/// MailDev exposes a small JSON HTTP API: <c>GET /email</c> lists every received
/// message, <c>DELETE /email/all</c> clears the inbox.
/// </summary>
internal static class MailDevHelper
{
    /// <summary>
    /// Clears the MailDev inbox and resets the Service Bus emulator to a clean
    /// state. Restarting the emulator container guarantees a fresh AMQP session
    /// for the worker, preventing stale connections that would cause subsequent
    /// tests to time out waiting for messages.
    /// </summary>
    public static async Task ClearAsync(this EndToEndTestEnvironment environment, CancellationToken ct)
    {
        await environment.MailDevClient.DeleteAsync("/email/all", ct);

        var commandService = environment.Application.Services
            .GetRequiredService<ResourceCommandService>();

        await commandService.ExecuteCommandAsync("messaging", "restart", ct);

        await environment.Application.ResourceNotifications.WaitForResourceAsync(
            "messaging",
            KnownResourceStates.Running,
            ct);

        // The container entering "Running" state does not guarantee the AMQP stack is
        // accepting connections yet. Probe the queue until a peek succeeds so that
        // subsequent test requests can rely on Service Bus being fully ready.
        await WaitForServiceBusReadyAsync(environment, ct);
    }

    /// <summary>
    /// Probes the Service Bus queue with a peek until the emulator's AMQP stack
    /// accepts the connection, or throws <see cref="TimeoutException"/> after 60 s.
    /// </summary>
    private static async Task WaitForServiceBusReadyAsync(EndToEndTestEnvironment environment, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var receiver = environment.ServiceBusClient.CreateReceiver("queue");

            using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            probeCts.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                await receiver.PeekMessageAsync(cancellationToken: probeCts.Token);
                return;
            }
            catch (OperationCanceledException) when (probeCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                // Probe timed out; SB not ready yet — retry after a brief wait.
            }
            catch (ServiceBusException)
            {
                // Connection error; SB not ready yet — retry after a brief wait.
            }

            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }

        throw new TimeoutException("Service Bus emulator was not ready within 60 seconds after restart.");
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
