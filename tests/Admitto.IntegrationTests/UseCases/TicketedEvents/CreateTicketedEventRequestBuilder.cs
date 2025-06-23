using Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;
using Amolenk.Admitto.Domain.ValueObjects;
using Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;

namespace Amolenk.Admitto.IntegrationTests.UseCases.TicketedEvents;

public class CreateTicketedEventRequestBuilder
{
    private string _name = "Test Event";
    private TeamId? _teamId = TeamBuilder.DefaultId;
    private DateTimeOffset? _registrationStartTime = DateTimeOffset.UtcNow.AddDays(1);
    private DateTimeOffset? _registrationEndTime = DateTimeOffset.UtcNow.AddDays(6);
    private DateTimeOffset? _startTime = DateTimeOffset.UtcNow.AddDays(7);
    private DateTimeOffset? _endTime = DateTimeOffset.UtcNow.AddDays(8);
    private List<TicketTypeDto> _ticketTypes = [
        new TicketTypeDto("General Admission", "Default", 100)
    ];

    public CreateTicketedEventRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CreateTicketedEventRequestBuilder WithTeamId(TeamId? teamId)
    {
        _teamId = teamId;
        return this;
    }

    public CreateTicketedEventRequestBuilder WithStartTime(DateTimeOffset? start)
    {
        _startTime = start;
        return this;
    }

    public CreateTicketedEventRequestBuilder WithEndTime(DateTimeOffset? end)
    {
        _endTime = end;
        return this;
    }

    public CreateTicketedEventRequestBuilder WithRegistrationStartTime(DateTimeOffset? regStart)
    {
        _registrationStartTime = regStart;
        return this;
    }

    public CreateTicketedEventRequestBuilder WithRegistrationEndTime(DateTimeOffset? regEnd)
    {
        _registrationEndTime = regEnd;
        return this;
    }

    public CreateTicketedEventRequestBuilder WithTicketTypes(IEnumerable<TicketTypeDto> ticketTypes)
    {
        _ticketTypes = new List<TicketTypeDto>(ticketTypes);
        return this;
    }

    public CreateTicketedEventRequest Build()
    {
        return new CreateTicketedEventRequest(
            Name: _name,
            TeamId: _teamId?.Value ?? Guid.Empty,
            StartTime: _startTime ?? default,
            EndTime: _endTime ?? default,
            RegistrationStartTime: _registrationStartTime ?? default,
            RegistrationEndTime: _registrationEndTime ?? default,
            TicketTypes: _ticketTypes
        );
    }
}