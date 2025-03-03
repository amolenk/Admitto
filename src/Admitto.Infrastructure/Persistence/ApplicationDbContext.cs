using System.Reflection;
using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<TicketedEvent> TicketedEvents { get; set; } = null!;

    public DbSet<OutboxMessage> Outbox { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
