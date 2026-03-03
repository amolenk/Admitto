// using Amolenk.Admitto.Organization.Contracts;
// using Amolenk.Admitto.Registrations.Application.Tests.Builders;
// using Amolenk.Admitto.Registrations.Application.Tests.Infrastructure.Hosting;
// using Amolenk.Admitto.Shared.Kernel.ValueObjects;
// using NSubstitute;
//
// namespace Amolenk.Admitto.Registrations.Application.Tests.UseCases.RegisterAttendee;
//
// internal sealed class RegisterAttendeeFixture
// {
//     private int _ticketCapacity = 10;
//     
//     public TicketedEventId EventId { get; } = new (Guid.Parse("11111111-1111-1111-1111-111111111111"));
//     public TicketTypeId TicketTypeId { get; } = new (Guid.Parse("22222222-2222-2222-2222-222222222222"));
//     public IOrganizationFacade OrganizationFacade { get; } = Substitute.For<IOrganizationFacade>();
//     
//     private RegisterAttendeeFixture()
//     {
//     }
//
//     public static RegisterAttendeeFixture HappyFlow() => new();
//
//     public static RegisterAttendeeFixture SoldOut() =>
//         new() { _ticketCapacity = 0 };
//
//     public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
//     {
//         var ticketTypeDto = new TicketTypeDto
//         {
//             Id = TicketTypeId.Value,
//             AdminLabel = "Admin Label",
//             PublicTitle = "Public Title",
//             TimeSlots = []
//         };
//         
//         // Mock organization facade to return ticket type information.
//         OrganizationFacade
//             .GetTicketTypesAsync(EventId.Value, Arg.Any<CancellationToken>())
//             .Returns([ticketTypeDto]);
//         
//         var ticketedEventCapacity = new TicketedEventCapacityBuilder()
//             .WithEventId(EventId)
//             .WithTicketTypeCapacity(TicketTypeId.Value, _ticketCapacity)
//             .Build();
//         
//         await environment.Database.SeedAsync(dbContext =>
//         {
//             dbContext.TicketedEventCapacities.Add(ticketedEventCapacity);
//         });
//     }
// }