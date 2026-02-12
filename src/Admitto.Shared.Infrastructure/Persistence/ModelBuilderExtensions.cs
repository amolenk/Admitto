using Amolenk.Admitto.Shared.Infrastructure.Persistence.ValueConverters;
using Amolenk.Admitto.Shared.Kernel.Abstractions;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Shared.Infrastructure.Persistence;

public static class ModelBuilderExtensions
{
    extension(ModelBuilder modelBuilder)
    {
        public void ApplySharedConfiguration()
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
            {
                if (typeof(IIsVersioned).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(nameof(IIsVersioned.Version))
                        .IsRowVersion();
                }

                if (typeof(IIsAuditable).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(nameof(IIsAuditable.CreatedAt))
                        .HasColumnName("created_at")
                        .IsRequired();

                    modelBuilder.Entity(entityType.ClrType)
                        .Property(nameof(IIsAuditable.LastChangedAt))
                        .HasColumnName("last_changed_at")
                        .IsRequired();

                    modelBuilder.Entity(entityType.ClrType)
                        .Property(nameof(IIsAuditable.LastChangedBy))
                        .HasColumnName("last_changed_by")
                        .HasMaxLength(EmailAddress.MaxLength)
                        .IsRequired();
                }
            }
        }
    }
    
    extension(ModelConfigurationBuilder modelConfigurationBuilder)
    {
        public void ConfigureSharedConventions()
        {
            modelConfigurationBuilder
                .Properties<DisplayName>()
                .HaveConversion<DisplayNameConverter>();

            modelConfigurationBuilder
                .Properties<EmailAddress>()
                .HaveConversion<EmailAddressConverter>();

            modelConfigurationBuilder
                .Properties<Slug>()
                .HaveConversion<SlugConverter>();

            modelConfigurationBuilder
                .Properties<TeamId>()
                .HaveConversion<TeamIdConverter>();
        }
    }
}