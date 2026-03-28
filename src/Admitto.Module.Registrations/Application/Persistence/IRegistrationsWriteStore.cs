using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.Persistence;

public interface IRegistrationsWriteStore
{
    DbSet<Coupon> Coupons { get; }

    // DbSet<RegistrationRecord> Registrations { get; }
    //
    // DbSet<EventCapacityRecord> EventCapacities { get; }
}