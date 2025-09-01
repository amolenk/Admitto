using System.Security.Cryptography;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents an event for which attendees can register.
/// </summary>
public class TicketedEvent : Aggregate
{
    // EF Core constructor
    private TicketedEvent()
    {
    }

    private TicketedEvent(
        Guid id,
        Guid teamId,
        string slug,
        string name,
        string website,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string baseUrl)
        : base(id)
    {
        TeamId = teamId;
        Slug = slug;
        Name = name;
        Website = website;
        StartTime = startTime;
        EndTime = endTime;
        BaseUrl = baseUrl;
        ConfiguredPolicies = new TicketedEventPolicies();
        SigningKey = GenerateHmacKey(32);
        

        AddDomainEvent(new TicketedEventCreatedDomainEvent(teamId, slug));
    }

    public Guid TeamId { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Website { get; private set; } = null!;
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public string BaseUrl { get; private set; } = null!;
    public TicketedEventPolicies ConfiguredPolicies { get; private set; } = null!;
    public string SigningKey { get; private set; } = null!;
    
    public RegistrationPolicy RegistrationPolicy => ConfiguredPolicies.RegistrationPolicy ?? RegistrationPolicy.Default;
    public CancellationPolicy CancellationPolicy => ConfiguredPolicies.CancellationPolicy ?? CancellationPolicy.Default;

    public static TicketedEvent Create(
        Guid teamId,
        string slug,
        string name,
        string website,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string baseUrl)
    {
        // TODO Additional validations

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainRuleException(DomainRuleError.TicketedEvent.NameIsRequired);

        if (endTime < startTime)
            throw new DomainRuleException(DomainRuleError.TicketedEvent.EndTimeMustBeAfterStartTime);

        return new TicketedEvent(
            Guid.NewGuid(),
            teamId,
            slug,
            name,
            website,
            startTime,
            endTime,
            baseUrl);
    }
    
    private static string GenerateHmacKey(int sizeInBytes = 32)
    {
        var key = new byte[sizeInBytes]; // 32 bytes = 256-bit key
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}