using Amolenk.Admitto.Shared.Kernel.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Shared.Infrastructure.Persistence;

public static class ModelBuilderExtensions
{
    extension(ModelBuilder modelBuilder)
    {
        public void ApplyDefaultConfiguration()
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
                }
            }
        }
    }
}