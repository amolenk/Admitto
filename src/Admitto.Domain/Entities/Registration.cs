using System.Security.Cryptography;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Exceptions;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents the registration for an event.
/// </summary>
public class Registration : AggregateRoot, IHasAdditionalDetails
{
    private readonly List<AdditionalDetail> _additionalDetails = [];
    private readonly List<TicketQuantity> _tickets = [];

    private Registration()
    {
    }

    private Registration(
        Guid id,
        Guid ticketedEventId,
        RegistrationType type,
        string email,
        string firstName,
        string lastName,
        Dictionary<string, string> additionalDetails,
        Dictionary<string, int> tickets)
        : base(id)
    {
        TicketedEventId = ticketedEventId;
        Type = type;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Status = type == RegistrationType.Public
            ? RegistrationStatus.PendingUserVerification
            : RegistrationStatus.PendingCompletion;

        _additionalDetails = additionalDetails
            .Select(item => new AdditionalDetail(item.Key, item.Value))
            .ToList();

        _tickets = tickets
            .Select(item => TicketQuantity.Create(item.Key, item.Value))
            .ToList();

        AddDomainEvent(new RegistrationReceivedDomainEvent(TicketedEventId, Id, type, tickets));
    }

    public Guid TicketedEventId { get; private set; }
    public RegistrationType Type { get; private set; }
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public RegistrationStatus Status { get; private set; }
    public IReadOnlyCollection<AdditionalDetail> AdditionalDetails => _additionalDetails.AsReadOnly();
    public IReadOnlyCollection<TicketQuantity> Tickets => _tickets.AsReadOnly();

    public static Registration Create(
        TicketedEventId ticketedEventId,
        RegistrationType type,
        string email,
        string firstName,
        string lastName,
        Dictionary<string, string>? additionalDetails = null,
        Dictionary<string, int>? tickets = null)
    {
        additionalDetails ??= new Dictionary<string, string>();
        tickets ??= new Dictionary<string, int>();

        return new Registration(
            Guid.NewGuid(),
            ticketedEventId,
            type,
            email,
            firstName,
            lastName,
            additionalDetails,
            tickets);
    }

    public void Verify()
    {
        if (Status == RegistrationStatus.PendingUserVerification)
        {
            Status = RegistrationStatus.PendingCompletion;
        }

        AddDomainEvent(
            new UserVerifiedRegistrationDomainEvent(
                TicketedEventId,
                Id,
                Type,
                _tickets.ToDictionary(x => x.Slug, x => x.Quantity)));
    }

    public void Complete()
    {
        if (Status != RegistrationStatus.PendingCompletion)
        {
            throw new BusinessRuleException("Cannot complete a registration that is not pending completion.");
        }

        Status = RegistrationStatus.Completed;

        AddDomainEvent(new RegistrationCompletedDomainEvent(Id));

        // Automatically reconfirm if the registration type is internal.
        if (Type == RegistrationType.Internal)
        {
            Reconfirm();
        }
    }

    public void Reject()
    {
        if (Status != RegistrationStatus.PendingCompletion)
        {
            throw new BusinessRuleException("Cannot reject a registration that is not pending completion.");
        }
        
        Status = RegistrationStatus.Rejected;
        
        
    }

    public void Reconfirm()
    {
        if (Status != RegistrationStatus.Completed) return;

        Status = RegistrationStatus.Reconfirmed;

//        AddDomainEvent(new RegistrationCompletedDomainEvent(Id));
    }

    // TODO Don't need it here, but maybe useful for the e-mail confirmation
    // private static string GenerateConfirmationCode()
    // {
    //     // Generates a random 6-digit code (000000-999999)
    //     var bytes = new byte[4];
    //     RandomNumberGenerator.Fill(bytes);
    //
    //     var value = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF; // Ensure non-negative
    //     var code = value % 1_000_000;
    //
    //     return code.ToString("D6");
    // }
}