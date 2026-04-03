// Temporary event lifecycle API methods until the NSwag client is regenerated.
// Delete this file after running: ./generate-api-client.sh

namespace Amolenk.Admitto.Cli.Api;

public partial class ApiClient
{
    // Override for the PUT-based update endpoint with ExpectedVersion.
    // The generated client targets the older PATCH endpoint; this overload uses PUT.
    public virtual System.Threading.Tasks.Task UpdateTicketedEventAsync(
        string teamSlug, string eventSlug, UpdateEventRequest body)
    {
        return UpdateTicketedEventAsync(teamSlug, eventSlug, body, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task UpdateTicketedEventAsync(
        string teamSlug,
        string eventSlug,
        UpdateEventRequest body,
        System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teamSlug);
        ArgumentNullException.ThrowIfNull(eventSlug);
        ArgumentNullException.ThrowIfNull(body);

        var client_ = _httpClient;
        using var request_ = new System.Net.Http.HttpRequestMessage();
        var json_ = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(body, JsonSerializerSettings);
        var content_ = new System.Net.Http.ByteArrayContent(json_);
        content_.Headers.ContentType =
            System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
        request_.Content = content_;
        request_.Method = new System.Net.Http.HttpMethod("PUT");

        var urlBuilder_ = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
        urlBuilder_.Append("teams/");
        urlBuilder_.Append(Uri.EscapeDataString(teamSlug));
        urlBuilder_.Append("/events/");
        urlBuilder_.Append(Uri.EscapeDataString(eventSlug));

        PrepareRequest(client_, request_, urlBuilder_);
        var url_ = urlBuilder_.ToString();
        request_.RequestUri = new Uri(url_, UriKind.RelativeOrAbsolute);
        PrepareRequest(client_, request_, url_);

        var response_ = await client_
            .SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var headers_ = GetResponseHeaders(response_);
            ProcessResponse(client_, response_);
            var status_ = (int)response_.StatusCode;

            if (status_ == 200)
            {
                return;
            }

            await ThrowOnUnexpectedStatusAsync<object>(response_, headers_, status_, cancellationToken);
        }
        finally
        {
            response_.Dispose();
        }
    }

    public virtual System.Threading.Tasks.Task CancelTicketedEventAsync(
        string teamSlug, string eventSlug, CancelTicketedEventRequest body)
    {
        return CancelTicketedEventAsync(teamSlug, eventSlug, body, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task CancelTicketedEventAsync(
        string teamSlug,
        string eventSlug,
        CancelTicketedEventRequest body,
        System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teamSlug);
        ArgumentNullException.ThrowIfNull(eventSlug);
        ArgumentNullException.ThrowIfNull(body);

        var client_ = _httpClient;
        using var request_ = new System.Net.Http.HttpRequestMessage();
        var json_ = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(body, JsonSerializerSettings);
        var content_ = new System.Net.Http.ByteArrayContent(json_);
        content_.Headers.ContentType =
            System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
        request_.Content = content_;
        request_.Method = new System.Net.Http.HttpMethod("POST");

        var urlBuilder_ = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
        urlBuilder_.Append("teams/");
        urlBuilder_.Append(Uri.EscapeDataString(teamSlug));
        urlBuilder_.Append("/events/");
        urlBuilder_.Append(Uri.EscapeDataString(eventSlug));
        urlBuilder_.Append("/cancel");

        PrepareRequest(client_, request_, urlBuilder_);
        var url_ = urlBuilder_.ToString();
        request_.RequestUri = new Uri(url_, UriKind.RelativeOrAbsolute);
        PrepareRequest(client_, request_, url_);

        var response_ = await client_
            .SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var headers_ = GetResponseHeaders(response_);
            ProcessResponse(client_, response_);
            var status_ = (int)response_.StatusCode;

            if (status_ == 200)
            {
                return;
            }

            await ThrowOnUnexpectedStatusAsync<object>(response_, headers_, status_, cancellationToken);
        }
        finally
        {
            response_.Dispose();
        }
    }

    public virtual System.Threading.Tasks.Task ArchiveTicketedEventAsync(
        string teamSlug, string eventSlug, ArchiveTicketedEventRequest body)
    {
        return ArchiveTicketedEventAsync(teamSlug, eventSlug, body, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task ArchiveTicketedEventAsync(
        string teamSlug,
        string eventSlug,
        ArchiveTicketedEventRequest body,
        System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teamSlug);
        ArgumentNullException.ThrowIfNull(eventSlug);
        ArgumentNullException.ThrowIfNull(body);

        var client_ = _httpClient;
        using var request_ = new System.Net.Http.HttpRequestMessage();
        var json_ = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(body, JsonSerializerSettings);
        var content_ = new System.Net.Http.ByteArrayContent(json_);
        content_.Headers.ContentType =
            System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
        request_.Content = content_;
        request_.Method = new System.Net.Http.HttpMethod("POST");

        var urlBuilder_ = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
        urlBuilder_.Append("teams/");
        urlBuilder_.Append(Uri.EscapeDataString(teamSlug));
        urlBuilder_.Append("/events/");
        urlBuilder_.Append(Uri.EscapeDataString(eventSlug));
        urlBuilder_.Append("/archive");

        PrepareRequest(client_, request_, urlBuilder_);
        var url_ = urlBuilder_.ToString();
        request_.RequestUri = new Uri(url_, UriKind.RelativeOrAbsolute);
        PrepareRequest(client_, request_, url_);

        var response_ = await client_
            .SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var headers_ = GetResponseHeaders(response_);
            ProcessResponse(client_, response_);
            var status_ = (int)response_.StatusCode;

            if (status_ == 200)
            {
                return;
            }

            await ThrowOnUnexpectedStatusAsync<object>(response_, headers_, status_, cancellationToken);
        }
        finally
        {
            response_.Dispose();
        }
    }
}

[System.CodeDom.Compiler.GeneratedCode("Manual", "1.0")]
public class UpdateEventRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("expectedVersion")]
    public uint? ExpectedVersion { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("startsAt")]
    public DateTimeOffset? StartsAt { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("endsAt")]
    public DateTimeOffset? EndsAt { get; set; }
}

[System.CodeDom.Compiler.GeneratedCode("Manual", "1.0")]
public class CancelTicketedEventRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("expectedVersion")]
    public uint? ExpectedVersion { get; set; }
}

[System.CodeDom.Compiler.GeneratedCode("Manual", "1.0")]
public class ArchiveTicketedEventRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("expectedVersion")]
    public uint? ExpectedVersion { get; set; }
}
