using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Registrations.Application.Persistence;

public interface IRegistrationsWriteStore
{
    DbSet<RegistrationRecord> Registrations { get; }
    
    DbSet<EventCapacityRecord> EventCapacities { get; }
}