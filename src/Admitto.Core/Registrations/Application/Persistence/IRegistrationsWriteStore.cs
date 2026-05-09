using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.Persistence;

public interface IRegistrationsWriteStore
{
    DbSet<ActivityLog> ActivityLog { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<OtpCode> OtpCodes { get; }
    DbSet<Registration> Registrations { get; }
    DbSet<TicketCatalog> TicketCatalogs { get; }
    DbSet<TicketedEvent> TicketedEvents { get; }
}