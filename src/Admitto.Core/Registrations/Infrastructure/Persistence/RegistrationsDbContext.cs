using System.Reflection;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;

public sealed class RegistrationsDbContext(DbContextOptions<RegistrationsDbContext> options)
    : DbContext(options), IModuleDbContext, IRegistrationsWriteStore, IOutboxDbContext
{
    public static string SchemaName => "registrations";

    public DbSet<ActivityLog> ActivityLog => Set<ActivityLog>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<TicketCatalog> TicketCatalogs => Set<TicketCatalog>();
    public DbSet<TicketedEvent> TicketedEvents => Set<TicketedEvent>();
    public DbSet<Waitlist> Waitlists => Set<Waitlist>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplySharedConfiguration();
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessedMessageEntityConfiguration());
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly(),
            t => t.Namespace?.StartsWith("Amolenk.Admitto.Core.Registrations") == true);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<EventName>()
            .HaveConversion<EventName.EfCoreValueConverter>();

        configurationBuilder
            .Properties<Slug>()
            .HaveConversion<Slug.EfCoreValueConverter>();

        configurationBuilder
            .Properties<TicketTypeName>()
            .HaveConversion<TicketTypeName.EfCoreValueConverter>();

        configurationBuilder
            .Properties<RegistrationId>()
            .HaveConversion<RegistrationId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<EmailAddress>()
            .HaveConversion<EmailAddress.EfCoreValueConverter>();

        configurationBuilder
            .Properties<TeamId>()
            .HaveConversion<TeamId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<TicketedEventId>()
            .HaveConversion<TicketedEventId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<AbsoluteUrl>()
            .HaveConversion<AbsoluteUrl.EfCoreValueConverter>();

        configurationBuilder
            .Properties<TimeZoneId>()
            .HaveConversion<TimeZoneId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<FirstName>()
            .HaveConversion<FirstName.EfCoreValueConverter>();

        configurationBuilder
            .Properties<LastName>()
            .HaveConversion<LastName.EfCoreValueConverter>();

        configurationBuilder
            .Properties<CouponId>()
            .HaveConversion<CouponId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<CouponCode>()
            .HaveConversion<CouponCode.EfCoreValueConverter>();

        configurationBuilder
            .Properties<OtpCodeId>()
            .HaveConversion<OtpCodeId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<ActivityLogId>()
            .HaveConversion<ActivityLogId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<TicketTypeId>()
            .HaveConversion<TicketTypeId.EfCoreValueConverter>();
    }
}
