using Amolenk.Admitto.Registrations.Domain.Entities;
using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Mapping;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Registrations.Application.Persistence;

public class RegistrationRecord
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public required string Email { get; set; }
    public required AttendeeInfoRecord AttendeeInfo { get; init; }
    public TicketRecord[] Tickets { get; init; } = [];
    
    public static RegistrationRecord FromDomain(Registration registration) => new()
    {
        Id = registration.Id.Value,
        EventId =  registration.EventId.Value,
        Email = registration.Email.Value,
        AttendeeInfo = AttendeeInfoRecord.FromDomain(registration.AttendeeInfo),
        Tickets = registration.Tickets.Select(TicketRecord.FromDomain).ToArray()
    };
    
    public void ApplyFromDomain(Registration registration)
    {
        Id = registration.Id.Value;
        EventId = registration.EventId.Value;
        Email = registration.Email.Value;
        
        AttendeeInfo.ApplyFromDomain(registration.AttendeeInfo);
        
        Tickets.ApplyFromDomain(
            registration.Tickets,
            domain => domain.TicketTypeId.Value,
            record => record.TicketTypeId,
            TicketRecord.FromDomain,
            (domain, record) => record.ApplyFromDomain(domain));
    }
    
    public Registration ToDomain() => Registration.Rehydrate(
        RegistrationId.From(Id),
        TicketedEventId.From(EventId),
        EmailAddress.From(Email),
        AttendeeInfo.ToDomain(),
        Tickets.Select(t => t.ToDomain()).ToList());
}
