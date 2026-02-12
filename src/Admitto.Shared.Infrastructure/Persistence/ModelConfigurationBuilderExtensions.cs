using Amolenk.Admitto.Shared.Infrastructure.Persistence.ValueConverters;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Shared.Infrastructure.Persistence;

public static class ModelConfigurationBuilderExtensions
{
    extension(ModelConfigurationBuilder modelConfigurationBuilder)
    {
        public void ConfigureSharedConventions()
        {
            modelConfigurationBuilder
                .Properties<TeamId>()
                .HaveConversion<TeamIdConverter>();

            modelConfigurationBuilder
                .Properties<EmailAddress>()
                .HaveConversion<EmailAddressConverter>();
        }
    }
}