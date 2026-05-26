using System.Text.Json.Serialization;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType.AdminApi;

public sealed class UpdateTicketTypeHttpRequest
{
    private int? _maxReconfirmAttempts;

    public string? Name { get; init; }
    public int? MaxCapacity { get; init; }
    public bool? SelfServiceEnabled { get; init; }
    public bool? WaitlistEnabled { get; init; }
    public int? ClaimWindowHours { get; init; }

    public int? MaxReconfirmAttempts
    {
        get => _maxReconfirmAttempts;
        init
        {
            _maxReconfirmAttempts = value;
            MaxReconfirmAttemptsSpecified = true;
        }
    }

    [JsonIgnore]
    internal bool MaxReconfirmAttemptsSpecified { get; private set; }

    internal UpdateTicketTypeCommand ToCommand(Guid eventId, Guid ticketTypeId) => new(
        eventId,
        ticketTypeId,
        Name,
        MaxCapacity,
        SelfServiceEnabled,
        WaitlistEnabled,
        ClaimWindowHours,
        MaxReconfirmAttempts,
        MaxReconfirmAttemptsSpecified);
}
