using System.Text.Json.Serialization;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.UpdateTicketType.AdminApi;

public sealed class UpdateTicketTypeHttpRequest
{
    private int? _maxReconfirmationEmails;

    public string? Name { get; init; }
    public int? MaxCapacity { get; init; }
    public bool? SelfServiceEnabled { get; init; }
    public bool? WaitlistEnabled { get; init; }
    public int? ClaimWindowHours { get; init; }

    public int? MaxReconfirmationEmails
    {
        get => _maxReconfirmationEmails;
        init
        {
            _maxReconfirmationEmails = value;
            MaxReconfirmationEmailsSpecified = true;
        }
    }

    [JsonIgnore]
    internal bool MaxReconfirmationEmailsSpecified { get; private set; }

    internal UpdateTicketTypeCommand ToCommand(Guid eventId, Guid teamId, Guid ticketTypeId) => new(
        eventId,
        teamId,
        ticketTypeId,
        Name,
        MaxCapacity,
        SelfServiceEnabled,
        WaitlistEnabled,
        ClaimWindowHours,
        MaxReconfirmationEmails,
        MaxReconfirmationEmailsSpecified);
}
