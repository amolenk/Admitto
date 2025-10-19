// using Amolenk.Admitto.Domain.Entities;
// using Amolenk.Admitto.Domain.ValueObjects;
//
// namespace Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;
//
// public class TicketedEventBuilder
// {
//     private Guid _id = Guid.NewGuid();
//     private TeamId _teamId = Guid.NewGuid();
//     private string _name = "Test Event";
//     private DateTime _startDate = DateTime.UtcNow.AddDays(7);
//     private DateTime _endDate = DateTime.UtcNow.AddDays(8);
//     private DateTime _registrationStartDate = DateTime.UtcNow.AddDays(1);
//     private DateTime _registrationEndDate = DateTime.UtcNow.AddDays(6);
//     private List<TicketType> _ticketTypes = [new TicketTypeBuilder().Build()];
//
//     public TicketedEventBuilder WithTeamId(TeamId teamId)
//     {
//         _teamId = teamId;
//         return this;
//     }
//
//     public TicketedEventBuilder WithName(string name)
//     {
//         _name = name;
//         return this;
//     }
//
//     public TicketedEventBuilder WithStartDate(DateTime startDate)
//     {
//         _startDate = startDate;
//         return this;
//     }
//
//     public TicketedEventBuilder WithEndDate(DateTime endDate)
//     {
//         _endDate = endDate;
//         return this;
//     }
//
//     public TicketedEventBuilder WithRegistrationStartDate(DateTime registrationStartDate)
//     {
//         _registrationStartDate = registrationStartDate;
//         return this;
//     }
//
//     public TicketedEventBuilder WithRegistrationEndDate(DateTime registrationEndDate)
//     {
//         _registrationEndDate = registrationEndDate;
//         return this;
//     }
//
//     public TicketedEventBuilder WithTicketTypes(List<TicketType> ticketTypes)
//     {
//         _ticketTypes = ticketTypes;
//         return this;
//     }
//
//     public TicketedEvent Build()
//     {
//         var ticketedEvent = TicketedEvent.Create(_teamId, _name, _startDate, _endDate,
//                 _registrationStartDate, _registrationEndDate);
//
//         foreach (var ticketType in _ticketTypes)
//         {
//             ticketedEvent.AddTicketType(ticketType.Name, ticketType.SlotName, ticketType.MaxCapacity);
//         }
//
//         return ticketedEvent;
//     }
// }