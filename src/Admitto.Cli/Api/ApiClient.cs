using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Api;

public partial class ApiClient
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.Converters.Add(new Iso8601TimeSpanConverter());
    }
}
