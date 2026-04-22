// Captures the last response's Location header so callers can read it after
// invoking generated client methods (NSwag's generated code discards headers).

namespace Amolenk.Admitto.Cli.Api;

public partial class ApiClient
{
    public Uri? LastResponseLocation { get; private set; }

    partial void ProcessResponse(System.Net.Http.HttpClient client, System.Net.Http.HttpResponseMessage response)
    {
        LastResponseLocation = response.Headers.Location;
    }
}
