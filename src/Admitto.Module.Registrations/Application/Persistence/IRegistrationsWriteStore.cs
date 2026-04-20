using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.Persistence;

public interface IRegistrationsWriteStore
{
    DbSet<Coupon> Coupons { get; }
    DbSet<Registration> Registrations { get; }
    DbSet<TicketCatalog> TicketCatalogs { get; }
    DbSet<EventRegistrationPolicy> EventRegistrationPolicies { get; }
    DbSet<TicketedEventLifecycleGuard> TicketedEventLifecycleGuards { get; }
    DbSet<CancellationPolicy> CancellationPolicies { get; }
    DbSet<ReconfirmPolicy> ReconfirmPolicies { get; }
}