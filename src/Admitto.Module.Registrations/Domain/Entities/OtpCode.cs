using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Abstractions;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

/// <summary>
/// Represents a one-time password code used to verify attendee email during self-service registration.
/// </summary>
public class OtpCode : Entity<OtpCodeId>, IIsVersioned, IDomainEventsProvider
{
    private readonly List<IDomainEvent> _domainEvents = [];

    // Required for EF Core
    // ReSharper disable once UnusedMember.Local
    private OtpCode()
    {
    }

    private OtpCode(
        OtpCodeId id,
        TicketedEventId eventId,
        TeamId teamId,
        string emailHash,
        string codeHash,
        DateTimeOffset expiresAt)
    {
        Id = id;
        EventId = eventId;
        TeamId = teamId;
        EmailHash = emailHash;
        CodeHash = codeHash;
        ExpiresAt = expiresAt;
        FailedAttempts = 0;
    }

    public TicketedEventId EventId { get; private set; }

    public TeamId TeamId { get; private set; }

    public string EmailHash { get; private set; } = null!;

    public string CodeHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public int FailedAttempts { get; private set; }

    public DateTimeOffset? SupersededAt { get; private set; }

    [Timestamp]
    public uint Version { get; set; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsUsed => UsedAt.HasValue;

    public bool IsLocked => FailedAttempts >= 5;

    public bool IsSuperseded => SupersededAt.HasValue;

    public static OtpCode Create(
        TeamId teamId,
        TicketedEventId eventId,
        string eventName,
        EmailAddress recipientEmail,
        string plainCode,
        DateTimeOffset expiresAt)
    {
        var id = OtpCodeId.New();
        var emailHash = HashValue(recipientEmail.Value.ToLowerInvariant());
        var codeHash = HashValue(plainCode);

        var otpCode = new OtpCode(id, eventId, teamId, emailHash, codeHash, expiresAt);

        otpCode._domainEvents.Add(new OtpCodeRequestedDomainEvent(
            id, teamId, eventId, eventName, recipientEmail, plainCode));

        return otpCode;
    }

    public void MarkUsed(DateTimeOffset now)
    {
        UsedAt = now;
    }

    public void IncrementFailedAttempts()
    {
        FailedAttempts++;
    }

    public void Supersede(DateTimeOffset now)
    {
        SupersededAt = now;
    }

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public static string ComputeEmailHash(string normalizedEmail)
        => HashValue(normalizedEmail);

    private static string HashValue(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(bytes);
    }
}
