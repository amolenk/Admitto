using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.Persistence;

public interface IRegistrationsWriteStore
{
    DbSet<ActivityLog> ActivityLog { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<Registration> Registrations { get; }
    DbSet<TicketCatalog> TicketCatalogs { get; }
    DbSet<TicketedEvent> TicketedEvents { get; }
}