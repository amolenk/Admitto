// using Amolenk.Admitto.Domain.Entities;
//
// namespace Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;
//
// public class TicketTypeBuilder
// {
//     private string _name = "Test Event";
//     private string _slotName = "Test Event";
//     private int _maxCapacity = 100;
//     
//     public TicketTypeBuilder WithName(string name)
//     {
//         _name = name;
//         return this;
//     }
//
//     public TicketTypeBuilder WithSlotName(string slotName)
//     {
//         _slotName = slotName;
//         return this;
//     }
//     
//     public TicketTypeBuilder WithMaxCapacity(int maxCapacity)
//     {
//         _maxCapacity = maxCapacity;
//         return this;
//     }
//
//     public TicketType Build() => TicketType.Create(_name, _slotName, _maxCapacity);
// }