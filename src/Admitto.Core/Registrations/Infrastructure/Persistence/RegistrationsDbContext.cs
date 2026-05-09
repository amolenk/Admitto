using System.Reflection;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;

public sealed class RegistrationsDbContext(DbContextOptions<RegistrationsDbContext> options)
    : DbContext(options), IModuleDbContext, IRegistrationsWriteStore, IOutboxDbContext
{
    public static string SchemaName => "registrations";

    public DbSet<ActivityLog> ActivityLog => Set<ActivityLog>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<TicketCatalog> TicketCatalogs => Set<TicketCatalog>();
    public DbSet<TicketedEvent> TicketedEvents => Set<TicketedEvent>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplySharedConfiguration();
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly(),
            t => t.Namespace?.StartsWith("Amolenk.Admitto.Core.Registrations") == true);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.ConfigureSharedConventions();
    }
}
