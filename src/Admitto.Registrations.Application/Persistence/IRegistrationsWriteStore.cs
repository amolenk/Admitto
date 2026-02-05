using Amolenk.Admitto.Registrations.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Registrations.Application.Persistence;

public interface IRegistrationsWriteStore
{
    DbSet<Registration> Registrations { get; }
    
    DbSet<TicketedEventCapacity> TicketedEventCapacities { get; }
}