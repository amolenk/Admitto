// Partial ApiClient extensions for Team Membership endpoints.
// Added manually because the current NSwag-generated client was built from an older
// OpenAPI spec that did not include these endpoints. Remove when the generated client
// is regenerated from a spec that includes the team membership routes.

namespace Amolenk.Admitto.Cli.Api;

public partial class ApiClient
{
    public virtual System.Threading.Tasks.Task<System.Collections.Generic.List<TeamMemberListItemResponse>>
        ListTeamMembersAsync(string teamSlug)
    {
        return ListTeamMembersAsync(teamSlug, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task<System.Collections.Generic.List<TeamMemberListItemResponse>>
        ListTeamMembersAsync(string teamSlug, System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teamSlug);

        var client_ = _httpClient;
        using var request_ = new System.Net.Http.HttpRequestMessage();
        request_.Method = new System.Net.Http.HttpMethod("GET");

        var urlBuilder_ = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
        // Operation Path: "teams/{teamSlug}/members"
        urlBuilder_.Append("teams/");
        urlBuilder_.Append(Uri.EscapeDataString(teamSlug));
        urlBuilder_.Append("/members");

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
                var stream_ = await response_.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var result_ = await System.Text.Json.JsonSerializer
                    .DeserializeAsync<System.Collections.Generic.List<TeamMemberListItemResponse>>(
                        stream_, JsonSerializerSettings, cancellationToken)
                    .ConfigureAwait(false);
                return result_!;
            }

            await ThrowOnUnexpectedStatusAsync<object>(response_, headers_, status_, cancellationToken);
            throw new InvalidOperationException("Unreachable");
        }
        finally
        {
            response_.Dispose();
        }
    }

    public virtual System.Threading.Tasks.Task AssignTeamMemberV2Async(
        string teamSlug,
        AssignTeamMemberV2Request body)
    {
        return AssignTeamMemberV2Async(teamSlug, body, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task AssignTeamMemberV2Async(
        string teamSlug,
        AssignTeamMemberV2Request body,
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
        // Operation Path: "teams/{teamSlug}/members"
        urlBuilder_.Append("teams/");
        urlBuilder_.Append(Uri.EscapeDataString(teamSlug));
        urlBuilder_.Append("/members");

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

            if (status_ == 200 || status_ == 204)
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

    public virtual System.Threading.Tasks.Task UpdateTeamMemberAsync(
        string teamSlug,
        string email,
        UpdateTeamMemberRequest body)
    {
        return UpdateTeamMemberAsync(teamSlug, email, body, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task UpdateTeamMemberAsync(
        string teamSlug,
        string email,
        UpdateTeamMemberRequest body,
        System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teamSlug);
        ArgumentNullException.ThrowIfNull(email);
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
        // Operation Path: "teams/{teamSlug}/members/{email}"
        urlBuilder_.Append("teams/");
        urlBuilder_.Append(Uri.EscapeDataString(teamSlug));
        urlBuilder_.Append("/members/");
        urlBuilder_.Append(Uri.EscapeDataString(email));

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

            if (status_ == 200 || status_ == 204)
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

    public virtual System.Threading.Tasks.Task RemoveTeamMemberAsync(string teamSlug, string email)
    {
        return RemoveTeamMemberAsync(teamSlug, email, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task RemoveTeamMemberAsync(
        string teamSlug,
        string email,
        System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teamSlug);
        ArgumentNullException.ThrowIfNull(email);

        var client_ = _httpClient;
        using var request_ = new System.Net.Http.HttpRequestMessage();
        request_.Method = new System.Net.Http.HttpMethod("DELETE");

        var urlBuilder_ = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
        // Operation Path: "teams/{teamSlug}/members/{email}"
        urlBuilder_.Append("teams/");
        urlBuilder_.Append(Uri.EscapeDataString(teamSlug));
        urlBuilder_.Append("/members/");
        urlBuilder_.Append(Uri.EscapeDataString(email));

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

            if (status_ == 200 || status_ == 204)
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
public class TeamMemberListItemResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("role")]
    public Amolenk.Admitto.Cli.Api.TeamMemberRole Role { get; set; }
}

[System.CodeDom.Compiler.GeneratedCode("Manual", "1.0")]
public class AssignTeamMemberV2Request
{
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("role")]
    public Amolenk.Admitto.Cli.Api.TeamMemberRole Role { get; set; }
}

[System.CodeDom.Compiler.GeneratedCode("Manual", "1.0")]
public class UpdateTeamMemberRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("newRole")]
    public Amolenk.Admitto.Cli.Api.TeamMemberRole NewRole { get; set; }
}
