using Amolenk.Admitto.Application.Common.DTOs;
using Amolenk.Admitto.Application.Common.ReadModels;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IApplicationContext
{
    DbSet<AttendeeActivityReadModel> AttendeeActivities { get; }

    DbSet<AttendeeRegistration> AttendeeRegistrations { get; }

    DbSet<OutboxMessageDto> Outbox { get; }

    DbSet<TicketedEvent> TicketedEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}