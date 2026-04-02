// Partial ApiClient extension for the ArchiveTeam endpoint.
// Added manually because the current NSwag-generated client was built from an older
// OpenAPI spec that did not include this endpoint. Remove when the generated client
// is regenerated from a spec that includes the ArchiveTeam route.

namespace Amolenk.Admitto.Cli.Api;

public partial class ApiClient
{
    public virtual System.Threading.Tasks.Task ArchiveTeamAsync(string teamSlug, ArchiveTeamRequest body)
    {
        return ArchiveTeamAsync(teamSlug, body, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task ArchiveTeamAsync(
        string teamSlug,
        ArchiveTeamRequest body,
        System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teamSlug);
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
        // Operation Path: "teams/{teamSlug}/archive"
        urlBuilder_.Append("teams/");
        urlBuilder_.Append(Uri.EscapeDataString(teamSlug));
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
public class ArchiveTeamRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("expectedVersion")]
    public uint ExpectedVersion { get; set; }
}
