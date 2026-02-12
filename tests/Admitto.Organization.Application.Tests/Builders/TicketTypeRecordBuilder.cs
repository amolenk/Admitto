// using Amolenk.Admitto.Organization.Application.Persistence;
//
// namespace Amolenk.Admitto.Organization.Application.Tests.Builders;
//
// public class TicketTypeRecordBuilder
// {
//     private Guid _id = Guid.NewGuid();
//     private string _adminLabel = "event-slug";
//     private string _publicTitle = "Event Name";
//     private bool _isSelfService;
//     private bool _isSelfServiceAvailable;
//     private List<string> _timeSlots = [];
//     private int? _capacity;
//
//     public TicketTypeRecordBuilder WithId(Guid id)
//     {
//         _id = id;
//         return this;
//     }
//
//     public TicketTypeRecordBuilder WithAdminLabel(string adminLabel)
//     {
//         _adminLabel = adminLabel;
//         return this;
//     }
//
//     public TicketTypeRecordBuilder WithPublicTitle(string publicTitle)
//     {
//         _publicTitle = publicTitle;
//         return this;
//     }
//
//     public TicketTypeRecordBuilder WithIsSelfService(bool isSelfService)
//     {
//         _isSelfService = isSelfService;
//         return this;
//     }
//
//     public TicketTypeRecordBuilder WithIsSelfServiceAvailable(bool isSelfServiceAvailable)
//     {
//         _isSelfServiceAvailable = isSelfServiceAvailable;
//         return this;
//     }
//
//     public TicketTypeRecordBuilder WithTimeSlots(params string[] timeSlots)
//     {
//         _timeSlots = timeSlots.ToList();
//         return this;
//     }
//
//     public TicketTypeRecordBuilder WithCapacity(int? capacity)
//     {
//         _capacity = capacity;
//         return this;
//     }
//
//     public TicketTypeRecord Build() => new()
//     {
//         Id = _id,
//         AdminLabel = _adminLabel,
//         PublicTitle = _publicTitle,
//         IsSelfService = _isSelfService,
//         IsSelfServiceAvailable = _isSelfServiceAvailable,
//         TimeSlots = _timeSlots,
//         Capacity = _capacity
//     };
// }