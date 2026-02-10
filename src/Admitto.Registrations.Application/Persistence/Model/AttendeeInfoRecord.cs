using Amolenk.Admitto.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Registrations.Application.Persistence;

public sealed record AttendeeInfoRecord
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    
    public static AttendeeInfoRecord FromDomain(AttendeeInfo attendeeInfo) => new()
    {
        FirstName = attendeeInfo.FirstName.Value,
        LastName = attendeeInfo.LastName.Value
    };

    public void ApplyFromDomain(AttendeeInfo attendeeInfo)
    {
        FirstName = attendeeInfo.FirstName.Value;
        LastName = attendeeInfo.LastName.Value;
    }
    
    public AttendeeInfo ToDomain() => new(
        Domain.ValueObjects.FirstName.From(FirstName),
        Domain.ValueObjects.LastName.From(LastName));
}
