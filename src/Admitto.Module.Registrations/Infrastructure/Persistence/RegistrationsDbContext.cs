// using System.Reflection;
// using Amolenk.Admitto.Module.Registrations.Application.Persistence;
// using Amolenk.Admitto.Module.Registrations.Domain.Entities;
// using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;
// using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;
// using Microsoft.EntityFrameworkCore;
//
// namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence;
//
// public sealed class RegistrationsDbContext(DbContextOptions<RegistrationsDbContext> options)
//     : DbContext(options), IModuleDbContext, IRegistrationsWriteStore, IOutboxDbContext
// {
//     public static string SchemaName => "registrations";
//
//     public DbSet<Registration> Registrations => Set<Registration>();
//
//     public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
//     
//     public DbSet<EventCapacity> TicketedEventCapacities => Set<EventCapacity>();
//     
//     protected override void OnModelCreating(ModelBuilder modelBuilder)
//     {
//         modelBuilder.HasDefaultSchema(SchemaName);
//         modelBuilder.ApplySharedConfiguration();
//         modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
//         modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
//     }
// }
