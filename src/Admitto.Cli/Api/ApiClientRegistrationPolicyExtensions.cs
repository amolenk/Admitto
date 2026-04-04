// Temporary registration policy API methods until the NSwag client is regenerated.
// Delete this file after running: ./generate-api-client.sh

namespace Amolenk.Admitto.Cli.Api;

public partial class ApiClient
{
    public virtual System.Threading.Tasks.Task UpdateRegistrationPolicyAsync(
        string teamSlug, string eventSlug, UpdateRegistrationPolicyRequest body)
    {
        return UpdateRegistrationPolicyAsync(teamSlug, eventSlug, body, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task UpdateRegistrationPolicyAsync(
        string teamSlug, string eventSlug, UpdateRegistrationPolicyRequest body,
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
        urlBuilder_.Append("admin/teams/");
        urlBuilder_.Append(Uri.EscapeDataString(teamSlug));
        urlBuilder_.Append("/events/");
        urlBuilder_.Append(Uri.EscapeDataString(eventSlug));
        urlBuilder_.Append("/registration-policy");

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

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "Temporary")]
public class UpdateRegistrationPolicyRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("registrationWindowOpensAt")]
    public DateTimeOffset? RegistrationWindowOpensAt { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("registrationWindowClosesAt")]
    public DateTimeOffset? RegistrationWindowClosesAt { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("allowedEmailDomain")]
    public string? AllowedEmailDomain { get; set; }
}
