// Temporary coupon API methods until the NSwag client is regenerated.
// Delete this file after running: ./generate-api-client.sh

namespace Amolenk.Admitto.Cli.Api;

public partial class ApiClient
{
    public virtual System.Threading.Tasks.Task<CreateCouponResponse> CreateCouponAsync(
        string teamSlug, string eventSlug, CreateCouponRequest body)
    {
        return CreateCouponAsync(teamSlug, eventSlug, body, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task<CreateCouponResponse> CreateCouponAsync(
        string teamSlug, string eventSlug, CreateCouponRequest body,
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
        request_.Headers.Accept.Add(
            System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));

        var urlBuilder_ = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
        urlBuilder_.Append("teams/");
        urlBuilder_.Append(Uri.EscapeDataString(teamSlug));
        urlBuilder_.Append("/events/");
        urlBuilder_.Append(Uri.EscapeDataString(eventSlug));
        urlBuilder_.Append("/coupons");

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

            if (status_ == 201)
            {
                var objectResponse_ =
                    await ReadObjectResponseAsync<CreateCouponResponse>(response_, headers_, cancellationToken)
                        .ConfigureAwait(false);
                return objectResponse_.Object
                       ?? throw new ApiException("Response was null which was not expected.", status_,
                           objectResponse_.Text, headers_, null);
            }

            return await ThrowOnUnexpectedStatusAsync<CreateCouponResponse>(response_, headers_, status_,
                cancellationToken);
        }
        finally
        {
            response_.Dispose();
        }
    }

    public virtual System.Threading.Tasks.Task<ListCouponsResponse> ListCouponsAsync(
        string teamSlug, string eventSlug)
    {
        return ListCouponsAsync(teamSlug, eventSlug, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task<ListCouponsResponse> ListCouponsAsync(
        string teamSlug, string eventSlug,
        System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teamSlug);
        ArgumentNullException.ThrowIfNull(eventSlug);

        var client_ = _httpClient;
        using var request_ = new System.Net.Http.HttpRequestMessage();
        request_.Method = new System.Net.Http.HttpMethod("GET");
        request_.Headers.Accept.Add(
            System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));

        var urlBuilder_ = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
        urlBuilder_.Append("teams/");
        urlBuilder_.Append(Uri.EscapeDataString(teamSlug));
        urlBuilder_.Append("/events/");
        urlBuilder_.Append(Uri.EscapeDataString(eventSlug));
        urlBuilder_.Append("/coupons");

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
                var objectResponse_ =
                    await ReadObjectResponseAsync<ListCouponsResponse>(response_, headers_, cancellationToken)
                        .ConfigureAwait(false);
                return objectResponse_.Object
                       ?? throw new ApiException("Response was null which was not expected.", status_,
                           objectResponse_.Text, headers_, null);
            }

            return await ThrowOnUnexpectedStatusAsync<ListCouponsResponse>(response_, headers_, status_,
                cancellationToken);
        }
        finally
        {
            response_.Dispose();
        }
    }

    public virtual System.Threading.Tasks.Task<GetCouponDetailsResponse> GetCouponDetailsAsync(
        string teamSlug, string eventSlug, Guid couponId)
    {
        return GetCouponDetailsAsync(teamSlug, eventSlug, couponId, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task<GetCouponDetailsResponse> GetCouponDetailsAsync(
        string teamSlug, string eventSlug, Guid couponId,
        System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teamSlug);
        ArgumentNullException.ThrowIfNull(eventSlug);

        var client_ = _httpClient;
        using var request_ = new System.Net.Http.HttpRequestMessage();
        request_.Method = new System.Net.Http.HttpMethod("GET");
        request_.Headers.Accept.Add(
            System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));

        var urlBuilder_ = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
        urlBuilder_.Append("teams/");
        urlBuilder_.Append(Uri.EscapeDataString(teamSlug));
        urlBuilder_.Append("/events/");
        urlBuilder_.Append(Uri.EscapeDataString(eventSlug));
        urlBuilder_.Append("/coupons/");
        urlBuilder_.Append(Uri.EscapeDataString(couponId.ToString()));

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
                var objectResponse_ =
                    await ReadObjectResponseAsync<GetCouponDetailsResponse>(response_, headers_, cancellationToken)
                        .ConfigureAwait(false);
                return objectResponse_.Object
                       ?? throw new ApiException("Response was null which was not expected.", status_,
                           objectResponse_.Text, headers_, null);
            }

            return await ThrowOnUnexpectedStatusAsync<GetCouponDetailsResponse>(response_, headers_, status_,
                cancellationToken);
        }
        finally
        {
            response_.Dispose();
        }
    }

    public virtual System.Threading.Tasks.Task RevokeCouponAsync(
        string teamSlug, string eventSlug, Guid couponId)
    {
        return RevokeCouponAsync(teamSlug, eventSlug, couponId, System.Threading.CancellationToken.None);
    }

    public virtual async System.Threading.Tasks.Task RevokeCouponAsync(
        string teamSlug, string eventSlug, Guid couponId,
        System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teamSlug);
        ArgumentNullException.ThrowIfNull(eventSlug);

        var client_ = _httpClient;
        using var request_ = new System.Net.Http.HttpRequestMessage();
        request_.Content = new System.Net.Http.StringContent(string.Empty);
        request_.Method = new System.Net.Http.HttpMethod("POST");

        var urlBuilder_ = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
        urlBuilder_.Append("teams/");
        urlBuilder_.Append(Uri.EscapeDataString(teamSlug));
        urlBuilder_.Append("/events/");
        urlBuilder_.Append(Uri.EscapeDataString(eventSlug));
        urlBuilder_.Append("/coupons/");
        urlBuilder_.Append(Uri.EscapeDataString(couponId.ToString()));
        urlBuilder_.Append("/revoke");

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

    private static Dictionary<string, IEnumerable<string>> GetResponseHeaders(
        System.Net.Http.HttpResponseMessage response)
    {
        var headers = new Dictionary<string, IEnumerable<string>>();
        foreach (var item in response.Headers)
            headers[item.Key] = item.Value;
        if (response.Content.Headers != null)
        {
            foreach (var item in response.Content.Headers)
                headers[item.Key] = item.Value;
        }

        return headers;
    }

    private async Task<T> ThrowOnUnexpectedStatusAsync<T>(
        System.Net.Http.HttpResponseMessage response,
        IReadOnlyDictionary<string, IEnumerable<string>> headers,
        int status,
        CancellationToken cancellationToken)
    {
        if (status == 400)
        {
            var obj = await ReadObjectResponseAsync<HttpValidationProblemDetails>(response, headers,
                cancellationToken).ConfigureAwait(false);
            if (obj.Object == null)
                throw new ApiException("Response was null which was not expected.", status, obj.Text, headers, null);
            throw new ApiException<HttpValidationProblemDetails>("Bad Request", status, obj.Text, headers,
                obj.Object, null);
        }

        if (status == 401)
        {
            var obj = await ReadObjectResponseAsync<ProblemDetails>(response, headers, cancellationToken)
                .ConfigureAwait(false);
            if (obj.Object == null)
                throw new ApiException("Response was null which was not expected.", status, obj.Text, headers, null);
            throw new ApiException<ProblemDetails>("Unauthorized", status, obj.Text, headers, obj.Object, null);
        }

        if (status == 403)
        {
            var obj = await ReadObjectResponseAsync<ProblemDetails>(response, headers, cancellationToken)
                .ConfigureAwait(false);
            if (obj.Object == null)
                throw new ApiException("Response was null which was not expected.", status, obj.Text, headers, null);
            throw new ApiException<ProblemDetails>("Forbidden", status, obj.Text, headers, obj.Object, null);
        }

        if (status == 409)
        {
            var obj = await ReadObjectResponseAsync<ProblemDetails>(response, headers, cancellationToken)
                .ConfigureAwait(false);
            if (obj.Object == null)
                throw new ApiException("Response was null which was not expected.", status, obj.Text, headers, null);
            throw new ApiException<ProblemDetails>("Conflict", status, obj.Text, headers, obj.Object, null);
        }

        var responseData = response.Content == null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new ApiException(
            "The HTTP status code of the response was not expected (" + status + ").",
            status, responseData, headers, null);
    }
}

// ── Request / Response DTOs (will be auto-generated after NSwag regeneration) ──

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "Temporary")]
public class CreateCouponRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string? Email { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("allowedTicketTypeSlugs")]
    public string[]? AllowedTicketTypeSlugs { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("bypassRegistrationWindow")]
    public bool BypassRegistrationWindow { get; set; }
}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "Temporary")]
public class CreateCouponResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("couponId")]
    public Guid CouponId { get; set; }
}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "Temporary")]
public class ListCouponsResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("coupons")]
    public IReadOnlyList<CouponSummaryDto>? Coupons { get; set; }
}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "Temporary")]
public class CouponSummaryDto
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public Guid Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string? Email { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("allowedTicketTypeSlugs")]
    public string[]? AllowedTicketTypeSlugs { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}

[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "Temporary")]
public class GetCouponDetailsResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public Guid Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public Guid Code { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string? Email { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("allowedTicketTypeSlugs")]
    public string[]? AllowedTicketTypeSlugs { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("bypassRegistrationWindow")]
    public bool BypassRegistrationWindow { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("redeemedAt")]
    public DateTimeOffset? RedeemedAt { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("revokedAt")]
    public DateTimeOffset? RevokedAt { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}
