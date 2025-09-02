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
        CancellationPolicy = CancellationPolicy.Default;
        RegistrationPolicy = RegistrationPolicy.Default;
        SigningKey = GenerateHmacKey(32);

        AddDomainEvent(new TicketedEventCreatedDomainEvent(teamId, Id, slug));
    }

    public Guid TeamId { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Website { get; private set; } = null!;
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public string BaseUrl { get; private set; } = null!;
    public CancellationPolicy CancellationPolicy { get; private set; } = null!;
    public ReconfirmPolicy? ReconfirmPolicy { get; private set; }
    public RegistrationPolicy RegistrationPolicy { get; private set; } = null!;
    public ReminderPolicy? ReminderPolicy { get; private set; }
    public string SigningKey { get; private set; } = null!;

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

    public void SetCancellationPolicy(CancellationPolicy policy)
    {
        CancellationPolicy = policy;
    }

    public void SetReconfirmPolicy(ReconfirmPolicy? policy)
    {
        ReconfirmPolicy = policy;
    }

    public void SetRegistrationPolicy(RegistrationPolicy policy)
    {
        RegistrationPolicy = policy;
    }

    public void SetReminderPolicy(ReminderPolicy? policy)
    {
        ReminderPolicy = policy;
    }

    private static string GenerateHmacKey(int sizeInBytes = 32)
    {
        var key = new byte[sizeInBytes]; // 32 bytes = 256-bit key
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}