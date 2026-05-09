using Amolenk.Admitto.Core.Module.Registrations.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.Persistence;

public interface IRegistrationsWriteStore
{
    DbSet<ActivityLog> ActivityLog { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<OtpCode> OtpCodes { get; }
    DbSet<Registration> Registrations { get; }
    DbSet<TicketCatalog> TicketCatalogs { get; }
    DbSet<TicketedEvent> TicketedEvents { get; }
}