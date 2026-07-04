using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.AppHost.Extensions;

public static class HostEnvironmentExtensions
{
    extension(IHostEnvironment hostEnvironment)
    {
        public bool IsIntegrationTesting()
        {
            return hostEnvironment.IsEnvironment("IntegrationTesting");
        }

        public bool IsEndToEndTesting()
        {
            return hostEnvironment.IsEnvironment("EndToEndTesting");
        }
    }
}