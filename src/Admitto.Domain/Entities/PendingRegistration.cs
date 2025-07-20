using System.Security.Cryptography;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Exceptions;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a request from a user to register for a ticketed event. Only after verification does the user become
/// an attendee.
/// </summary>
public class PendingRegistration : AggregateRoot, IHasAdditionalDetails
{
    private readonly List<AdditionalDetail> _additionalDetails = [];
    private readonly List<TicketSelection> _tickets = [];

    private PendingRegistration()
    {
    }

    private PendingRegistration(
        Guid id,
        Guid teamId,
        Guid ticketedEventId,
        string email,
        string firstName,
        string lastName,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets)
        : base(id)
    {
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Status = RegistrationRequestStatus.Unverified;
        ConfirmationCode = GenerateConfirmationCode();
        ExpirationTime = DateTime.UtcNow.AddMinutes(10);

        _additionalDetails = additionalDetails;
        _tickets = tickets;

        AddDomainEvent(new PendingRegistrationReceivedDomainEvent(TeamId, TicketedEventId, Id));
    }

    public Guid TeamId { get; private set; }
    public Guid TicketedEventId { get; private set; }
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public RegistrationRequestStatus Status { get; private set; }
    public string ConfirmationCode { get; private set; } = null!;
    public DateTime ExpirationTime { get; private set; }
    public IReadOnlyCollection<AdditionalDetail> AdditionalDetails => _additionalDetails.AsReadOnly();
    public IReadOnlyCollection<TicketSelection> Tickets => _tickets.AsReadOnly();

    public static PendingRegistration Create(
        Guid teamId,
        Guid ticketedEventId,
        string email,
        string firstName,
        string lastName,
        IEnumerable<AdditionalDetail> additionalDetails,
        IEnumerable<TicketSelection> tickets)
    {
        return new PendingRegistration(
            Guid.NewGuid(),
            teamId,
            ticketedEventId,
            email,
            firstName,
            lastName,
            additionalDetails.ToList(),
            tickets.ToList());
    }
    
    private bool IsExpired => DateTime.UtcNow > ExpirationTime;

    public void Verify(string confirmationCode)
    {
        // If the registration is already verified, throw an exception.
        if (Status != RegistrationRequestStatus.Unverified)
        {
            throw new BusinessRuleException("Registration request has already been verified or is not valid.");
        }
        
        // If the registration is expired, throw an exception.
        if (IsExpired)
        {
            throw new BusinessRuleException("Registration request has expired. Please start a new registration.");
        }
        
        // If the confirmation code does not match, throw an exception.
        if (ConfirmationCode != confirmationCode)
        {
            throw new BusinessRuleException("Invalid confirmation code. Please try again.");
        }
        
        // Mark the registration as verified.
        Status = RegistrationRequestStatus.Verified;
        
        AddDomainEvent(new PendingRegistrationVerifiedDomainEvent(TicketedEventId, Id));
    }

    public void Accept()
    {
        if (Status != RegistrationRequestStatus.Verified)
        {
            throw new BusinessRuleException("Registration request has already been completed.");
        }
        
        Status = RegistrationRequestStatus.Accepted;

        AddDomainEvent(new PendingRegistrationAcceptedDomainEvent(TicketedEventId, Id));
    }

    public void Reject()
    {
        if (Status != RegistrationRequestStatus.Verified)
        {
            throw new BusinessRuleException("Registration request has already been completed.");
        }
        
        Status = RegistrationRequestStatus.Rejected;
        
        AddDomainEvent(new PendingRegistrationRejectedDomainEvent(TicketedEventId, Id));
    }

    private static string GenerateConfirmationCode()
    {
        // Generates a random 6-digit code (000000-999999)
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
    
        var value = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF; // Ensure non-negative
        var code = value % 1_000_000;
    
        return code.ToString("D6");
    }
}