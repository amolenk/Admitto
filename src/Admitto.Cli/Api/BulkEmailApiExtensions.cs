// Manual partial extensions for the bulk-email admin endpoints.
//
// The NSwag-generated client contains broken request DTOs for the bulk-email
// create endpoint: the inline RegistrationStatus enum on
// AttendeeSourceHttpDto was emitted as an empty placeholder class
// ("RegistrationStatus2"/"RegistrationStatus3") instead of a string-typed
// enum, so the generated typed methods cannot transmit a registration-status
// filter. These manual partials bypass those types with hand-rolled request
// DTOs that match the OpenAPI contract and serialise correctly.

using System.Text.Json.Serialization;

namespace Amolenk.Admitto.Cli.Api;

public partial class ApiClient
{
    public virtual System.Threading.Tasks.Task<CreateBulkEmailResponse> CreateBulkEmailV2Async(
        string teamSlug,
        string eventSlug,
        CreateBulkEmailCliRequest body,
        System.Threading.CancellationToken cancellationToken)
    {
        return SendBulkEmailJsonAsync<CreateBulkEmailCliRequest, CreateBulkEmailResponse>(
            "POST",
            $"admin/teams/{Uri.EscapeDataString(teamSlug)}/events/{Uri.EscapeDataString(eventSlug)}/bulk-emails",
            body,
            cancellationToken);
    }

    private async System.Threading.Tasks.Task<TResponse> SendBulkEmailJsonAsync<TRequest, TResponse>(
        string method,
        string relativePath,
        TRequest body,
        System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        var client_ = _httpClient;
        using var request_ = new System.Net.Http.HttpRequestMessage();
        var json_ = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(body, JsonSerializerSettings);
        var content_ = new System.Net.Http.ByteArrayContent(json_);
        content_.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
        request_.Content = content_;
        request_.Method = new System.Net.Http.HttpMethod(method);

        var urlBuilder_ = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
        urlBuilder_.Append(relativePath);

        PrepareRequest(client_, request_, urlBuilder_);
        var url_ = urlBuilder_.ToString();
        request_.RequestUri = new Uri(url_, UriKind.RelativeOrAbsolute);
        PrepareRequest(client_, request_, url_);

        var response_ = await client_
            .SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var headers_ = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>>();
            foreach (var item_ in response_.Headers) headers_[item_.Key] = item_.Value;
            if (response_.Content is { Headers: var contentHeaders_ })
            {
                foreach (var item_ in contentHeaders_) headers_[item_.Key] = item_.Value;
            }

            ProcessResponse(client_, response_);
            var status_ = (int)response_.StatusCode;

            if (status_ is 200 or 201 or 202)
            {
                var stream_ = await response_.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                if (stream_.CanSeek && stream_.Length == 0)
                {
                    return default!;
                }

                var result_ = await System.Text.Json.JsonSerializer
                    .DeserializeAsync<TResponse>(stream_, JsonSerializerSettings, cancellationToken)
                    .ConfigureAwait(false);
                return result_!;
            }

            var responseText_ = response_.Content == null
                ? string.Empty
                : await response_.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new ApiException(
                "The HTTP status code of the response was not expected (" + status_ + ").",
                status_,
                responseText_,
                headers_,
                null);
        }
        finally
        {
            response_.Dispose();
        }
    }
}

[System.CodeDom.Compiler.GeneratedCode("Manual", "1.0")]
public sealed class CreateBulkEmailCliRequest
{
    [JsonPropertyName("emailType")]
    public string EmailType { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("textBody")]
    public string? TextBody { get; set; }

    [JsonPropertyName("htmlBody")]
    public string? HtmlBody { get; set; }

    [JsonPropertyName("source")]
    public BulkEmailSourceCliRequest Source { get; set; } = new();
}

[System.CodeDom.Compiler.GeneratedCode("Manual", "1.0")]
public sealed class BulkEmailSourceCliRequest
{
    [JsonPropertyName("attendee")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AttendeeSourceCliRequest? Attendee { get; set; }

    [JsonPropertyName("externalList")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExternalListSourceCliRequest? ExternalList { get; set; }
}

[System.CodeDom.Compiler.GeneratedCode("Manual", "1.0")]
public sealed class AttendeeSourceCliRequest
{
    [JsonPropertyName("ticketTypeSlugs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? TicketTypeSlugs { get; set; }

    /// <summary>
    /// Serialised as the camelCase string "registered" or "cancelled" — matches
    /// the API's <c>JsonStringEnumConverter(JsonNamingPolicy.CamelCase)</c>.
    /// </summary>
    [JsonPropertyName("registrationStatus")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegistrationStatus { get; set; }

    [JsonPropertyName("hasReconfirmed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HasReconfirmed { get; set; }

    [JsonPropertyName("registeredAfter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? RegisteredAfter { get; set; }

    [JsonPropertyName("registeredBefore")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? RegisteredBefore { get; set; }

    [JsonPropertyName("additionalDetailEquals")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? AdditionalDetailEquals { get; set; }
}

[System.CodeDom.Compiler.GeneratedCode("Manual", "1.0")]
public sealed class ExternalListSourceCliRequest
{
    [JsonPropertyName("items")]
    public IReadOnlyList<ExternalListRecipientCliRequest> Items { get; set; } = [];
}

[System.CodeDom.Compiler.GeneratedCode("Manual", "1.0")]
public sealed class ExternalListRecipientCliRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
}
